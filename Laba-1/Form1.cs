using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace Laba_1
{
    public partial class Compiler : Form
    {
        public Compiler()
        {
            InitializeComponent();
            tabControl1.TabPages.Clear();
            tabControl1.DrawMode = TabDrawMode.OwnerDrawFixed;
            tabControl1.DrawItem += tabControl1_DrawItem;
            tabControl1.MouseDown += tabControl1_MouseDown;
        }

        // Метод для создания новой вкладки с RichTextBox
        private void CreateNewTab(string tabTitle)
        {
            TabPage newTab = new TabPage(tabTitle);
            RichTextBox rtb = new RichTextBox { Dock = DockStyle.Fill };

            // Создаём объект DocumentInfo для хранения пути к файлу и статуса изменений
            DocumentInfo docInfo = new DocumentInfo();
            rtb.Tag = docInfo;

            // При изменении текста помечаем документ как изменённый и добавляем звездочку в заголовок вкладки
            rtb.TextChanged += (s, e) =>
            {
                docInfo.IsModified = true;
                if (!newTab.Text.EndsWith("*"))
                    newTab.Text += "*";
            };

            newTab.Controls.Add(rtb);
            tabControl1.TabPages.Add(newTab);
            tabControl1.SelectedTab = newTab;
        }

        // Метод отрисовки вкладки с крестиком
        private void tabControl1_DrawItem(object sender, DrawItemEventArgs e)
        {
            try
            {
                TabPage tabPage = tabControl1.TabPages[e.Index];
                Rectangle tabRect = tabControl1.GetTabRect(e.Index);
                // Отрисовка текста вкладки
                TextRenderer.DrawText(e.Graphics, tabPage.Text, tabControl1.Font,
                    new Point(tabRect.X + 2, tabRect.Y + 4), SystemColors.ControlText);

                // Определяем размеры крестика
                int closeButtonSize = 15;
                Rectangle closeButtonRect = new Rectangle(
                    tabRect.Right - closeButtonSize - 5,
                    tabRect.Top + (tabRect.Height - closeButtonSize) / 2,
                    closeButtonSize, closeButtonSize);

                // Отрисовка крестика (простой вариант)
                e.Graphics.DrawRectangle(Pens.Black, closeButtonRect);
                e.Graphics.DrawLine(Pens.Black, closeButtonRect.X, closeButtonRect.Y,
                    closeButtonRect.Right, closeButtonRect.Bottom);
                e.Graphics.DrawLine(Pens.Black, closeButtonRect.Right, closeButtonRect.Y,
                    closeButtonRect.X, closeButtonRect.Bottom);
            }
            catch (Exception ex)
            {
                // Обработка ошибок отрисовки (если потребуется)
            }
        }

        // Обработчик клика мыши для определения нажатия на крестик
        private void tabControl1_MouseDown(object sender, MouseEventArgs e)
        {
            for (int i = 0; i < tabControl1.TabPages.Count; i++)
            {
                Rectangle tabRect = tabControl1.GetTabRect(i);
                int closeButtonSize = 15;
                Rectangle closeButtonRect = new Rectangle(
                    tabRect.Right - closeButtonSize - 5,
                    tabRect.Top + (tabRect.Height - closeButtonSize) / 2,
                    closeButtonSize, closeButtonSize);

                if (closeButtonRect.Contains(e.Location))
                {
                    TabPage tab = tabControl1.TabPages[i];
                    // Если документ изменён, спрашиваем о сохранении
                    if (tab.Controls.Count > 0 && tab.Controls[0] is RichTextBox rtb)
                    {
                        DocumentInfo docInfo = rtb.Tag as DocumentInfo;
                        if (docInfo != null && docInfo.IsModified)
                        {
                            DialogResult dr = MessageBox.Show(
                                $"Сохранить изменения в \"{tab.Text.TrimEnd('*')}\"?",
                                "Сохранение", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning);
                            if (dr == DialogResult.Yes)
                            {
                                // Вызываем ваш метод сохранения (например, тот же, что используется в меню)
                                сохранитьToolStripMenuItem_Click(sender, e);
                            }
                            else if (dr == DialogResult.Cancel)
                            {
                                return; // Отмена закрытия вкладки
                            }
                        }
                    }
                    // Закрываем вкладку
                    tabControl1.TabPages.Remove(tab);
                    break;
                }
            }
        }

        // Обработчик пункта меню "Создать"
        private void создатьToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CreateNewTab("Новый документ");
        }

        // Обработчик пункта меню "Открыть"
        private void открытьToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Filter = "Текстовые файлы (*.txt)|*.txt|Все файлы (*.*)|*.*";
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    TabPage newTab = new TabPage(Path.GetFileName(ofd.FileName));
                    RichTextBox rtb = new RichTextBox { Dock = DockStyle.Fill };

                    DocumentInfo docInfo = new DocumentInfo
                    {
                        FilePath = ofd.FileName,
                        IsModified = false
                    };
                    rtb.Tag = docInfo;

                    try
                    {
                        rtb.Text = File.ReadAllText(ofd.FileName);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Ошибка при открытии файла: " + ex.Message);
                        return;
                    }

                    rtb.TextChanged += (s, ev) =>
                    {
                        docInfo.IsModified = true;
                        if (!newTab.Text.EndsWith("*"))
                            newTab.Text += "*";
                    };

                    newTab.Controls.Add(rtb);
                    tabControl1.TabPages.Add(newTab);
                    tabControl1.SelectedTab = newTab;
                }
            }
        }

        // Обработчик пункта меню "Сохранить"
        private void сохранитьToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (tabControl1.TabPages.Count == 0)
                return;

            TabPage activeTab = tabControl1.SelectedTab;
            if (activeTab == null || activeTab.Controls.Count == 0)
                return;

            RichTextBox rtb = activeTab.Controls[0] as RichTextBox;
            DocumentInfo docInfo = rtb.Tag as DocumentInfo;

            // Если путь не задан, вызываем "Сохранить как"
            if (string.IsNullOrEmpty(docInfo.FilePath))
            {
                сохранитьКакToolStripMenuItem_Click(sender, e);
            }
            else
            {
                SaveDocument(rtb, docInfo, activeTab);
            }
        }

        // Метод сохранения документа по указанному пути
        private void SaveDocument(RichTextBox rtb, DocumentInfo docInfo, TabPage tab)
        {
            try
            {
                File.WriteAllText(docInfo.FilePath, rtb.Text);
                docInfo.IsModified = false;
                if (tab.Text.EndsWith("*"))
                    tab.Text = tab.Text.TrimEnd('*');
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при сохранении файла: " + ex.Message);
            }
        }

        // Обработчик пункта меню "Сохранить как"
        private void сохранитьКакToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (tabControl1.TabPages.Count == 0)
                return;

            TabPage activeTab = tabControl1.SelectedTab;
            if (activeTab == null || activeTab.Controls.Count == 0)
                return;

            RichTextBox rtb = activeTab.Controls[0] as RichTextBox;
            DocumentInfo docInfo = rtb.Tag as DocumentInfo;

            using (SaveFileDialog sfd = new SaveFileDialog())
            {
                sfd.Filter = "Текстовые файлы (*.txt)|*.txt|Все файлы (*.*)|*.*";
                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    docInfo.FilePath = sfd.FileName;
                    SaveDocument(rtb, docInfo, activeTab);
                    activeTab.Text = Path.GetFileName(sfd.FileName);
                }
            }
        }

        // Обработчик пункта меню "Выход"
        private void выходToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // Проходим по всем вкладкам и, если найдены несохранённые изменения, предлагаем сохранить их
            foreach (TabPage tab in tabControl1.TabPages)
            {
                if (tab.Controls.Count == 0)
                    continue;
                RichTextBox rtb = tab.Controls[0] as RichTextBox;
                DocumentInfo docInfo = rtb.Tag as DocumentInfo;
                if (docInfo.IsModified)
                {
                    DialogResult dr = MessageBox.Show(
                        $"Сохранить изменения в \"{tab.Text.TrimEnd('*')}\"?",
                        "Сохранение",
                        MessageBoxButtons.YesNoCancel,
                        MessageBoxIcon.Warning);
                    if (dr == DialogResult.Yes)
                    {
                        tabControl1.SelectedTab = tab;
                        сохранитьToolStripMenuItem_Click(sender, e);
                    }
                    else if (dr == DialogResult.Cancel)
                    {
                        return;
                    }
                }
            }
            Application.Exit();
        }

        // Обработчик для пункта "Отменить"
        private void отменитьToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (tabControl1.TabPages.Count == 0)
                return;

            TabPage activeTab = tabControl1.SelectedTab;
            RichTextBox rtb = activeTab.Controls[0] as RichTextBox;
            if (rtb != null && rtb.CanUndo)
            {
                rtb.Undo();
            }
        }

        // Обработчик для пункта "Повторить"
        private void повторитьToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (tabControl1.TabPages.Count == 0)
                return;

            TabPage activeTab = tabControl1.SelectedTab;
            RichTextBox rtb = activeTab.Controls[0] as RichTextBox;
            if (rtb != null && rtb.CanRedo)
            {
                rtb.Redo();
            }
        }

        // Обработчик для пункта "Вырезать"
        private void вырезатьToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (tabControl1.TabPages.Count == 0)
                return;

            TabPage activeTab = tabControl1.SelectedTab;
            RichTextBox rtb = activeTab.Controls[0] as RichTextBox;
            if (rtb != null)
            {
                rtb.Cut();
            }
        }

        // Обработчик для пункта "Копировать"
        private void копироватьToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (tabControl1.TabPages.Count == 0)
                return;

            TabPage activeTab = tabControl1.SelectedTab;
            RichTextBox rtb = activeTab.Controls[0] as RichTextBox;
            if (rtb != null)
            {
                rtb.Copy();
            }
        }

        // Обработчик для пункта "Вставить"
        private void вставитьToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (tabControl1.TabPages.Count == 0)
                return;

            TabPage activeTab = tabControl1.SelectedTab;
            RichTextBox rtb = activeTab.Controls[0] as RichTextBox;
            if (rtb != null)
            {
                rtb.Paste();
            }
        }

        // Обработчик для пункта "Удалить"
        // Здесь мы просто удаляем выделенный текст (аналог действия "Delete")
        private void удалитьToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (tabControl1.TabPages.Count == 0)
                return;

            TabPage activeTab = tabControl1.SelectedTab;
            RichTextBox rtb = activeTab.Controls[0] as RichTextBox;
            if (rtb != null)
            {
                rtb.SelectedText = "";
            }
        }

        // Обработчик для пункта "Выделить все"
        private void выделитьВсеToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (tabControl1.TabPages.Count == 0)
                return;

            TabPage activeTab = tabControl1.SelectedTab;
            RichTextBox rtb = activeTab.Controls[0] as RichTextBox;
            if (rtb != null)
            {
                rtb.SelectAll();
            }
        }

        private void вызовСправкиToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // Получаем полный путь к файлу help.txt, который находится в папке запуска приложения
            string helpFilePath = System.IO.Path.Combine(Application.StartupPath, "help.txt");

            if (System.IO.File.Exists(helpFilePath))
            {
                try
                {
                    System.Diagnostics.Process.Start(helpFilePath);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Не удалось открыть справку: " + ex.Message);
                }
            }
            else
            {
                MessageBox.Show("Файл справки не найден.");
            }
        }

        private void splitContainer1_Panel2_Paint(object sender, PaintEventArgs e)
        {

        }

        private void outputTextBox_TextChanged(object sender, EventArgs e)
        {

        }
    }

    // Вспомогательный класс для хранения информации о документе
    public class DocumentInfo
    {
        public string FilePath { get; set; } = string.Empty;
        public bool IsModified { get; set; } = false;
    }
}
