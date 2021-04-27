using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using GmailScraper.Properties;
using Newtonsoft.Json;

namespace GmailScraper
{
    public partial class Form1 : Form
    {
        private List<string[]> _currentList;
        private int? _editingFieldIndex;

        public Form1()
        {
            this.InitializeComponent();
            textBox1.Text = Settings.Default.LastSearch;
            if (!string.IsNullOrEmpty(Settings.Default.FieldsToFind))
            {
                Program.FieldsList = JsonConvert.DeserializeObject<List<FieldToFind>>(
                    Settings.Default.FieldsToFind);
                this.SetFieldsToFind();
            }
        }

        private async void btnGo_Click(object sender, EventArgs e)
        {
            Settings.Default.LastSearch = textBox1.Text;
            Settings.Default.Save();

            Program.Stop = false;
            listView1.Items.Clear();
            listView1.Refresh();
            label2.Text = "Searching....";
            this.Cursor = Cursors.WaitCursor;
            _currentList = await Program.GetEmailsAndScrapeThem(textBox1.Text,
                i =>
                {
                    if (this.Cursor != Cursors.Default)
                    {
                        this.Cursor = Cursors.Default;
                    }
                    if (Program.Stop)
                    {
                        label2.Text = "...STOPPED...";
                        return;
                    }

                    listView1.Items.Add(new ListViewItem(i));
                    listView1.Refresh();
                    listView1.EnsureVisible(listView1.Items.Count - 1);
                    label2.Text = listView1.Items.Count + " emails processed";
                    label2.Refresh();
                });
            if (this.Cursor != Cursors.Default)
            {
                this.Cursor = Cursors.Default;
            }
            this.DownloadCsv();
            if (Program.Stop)
            {
            }
        }

        private void DownloadCsv()
        {
            if (_currentList == null)
            {
                return;
            }

            var csv = $"\"{string.Join("\",\"", Program.FieldsList.Select(i => i.Name))}\"\n";
            foreach (string[] row in _currentList)
            {
                csv += $"\"{string.Join("\",\"", row)}\"\n";
            }

            var fsd = new SaveFileDialog
            {
                DefaultExt = "csv",
                Filter = "CSV files (*.csv)|*.csv| All files (*.*)|*.*"
            };
            if (fsd.ShowDialog() == DialogResult.OK && !string.IsNullOrWhiteSpace(fsd.FileName))
            {
                File.WriteAllText(fsd.FileName, csv);
                MessageBox.Show($"The file was saved as \"{fsd.FileName}\"",
                    "Save email scrape results");
            }
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            Program.Stop = true;
            label2.Text = "...STOPPED...";
        }

        private void btnShowAddNewPanel_Click(object sender, EventArgs e)
        {
            txtName.Text = "";
            txtRegexPattern.Text = "";
            numericUpDown1.Value = 1;
            cbRequired.Checked = false;
            btnNewAdd.Text = "Add this Field";
            pnlNewField.Visible = true;
        }

        private void SetFieldsToFind()
        {
            listView1.Columns.Clear();
            listView2.Items.Clear();
            foreach (FieldToFind f in Program.FieldsList)
            {
                listView1.Columns.Add(f.Name, listView1.Width / Program.FieldsList.Count);
                listView2.Items.Add(new ListViewItem(new[]
                {
                    f.Required ? "Yes" : "No",
                    f.Name,
                    f.Regex,
                    f.GroupNumber.ToString()
                }));
            }
        }

        private void btnNewCancel_Click(object sender, EventArgs e)
        {
            pnlNewField.Visible = false;
        }

        private void btnNewAdd_Click(object sender, EventArgs e)
        {
            switch (_editingFieldIndex)
            {
                case null when Program.FieldsList.Any(fli => fli.Name == txtName.Text):
                    MessageBox.Show(
                        $@"There already is a field named {txtName.Text}. Field names should be unique.",
                        @"Gmail Scraper - add field",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Exclamation);
                    return;
                case null:
                    Program.FieldsList.Add(new FieldToFind
                    {
                        Name = txtName.Text,
                        Regex = txtRegexPattern.Text,
                        GroupNumber = (int)numericUpDown1.Value,
                        Required = cbRequired.Checked
                    });
                    break;
                default:
                    {
                        FieldToFind f = Program.FieldsList[_editingFieldIndex.Value];
                        f.Name = txtName.Text;
                        f.Regex = txtRegexPattern.Text;
                        f.GroupNumber = (int)numericUpDown1.Value;
                        f.Required = cbRequired.Checked;
                        _editingFieldIndex = null;
                        break;
                    }
            }

            this.SetFieldsToFind();
            pnlNewField.Visible = false;
            Settings.Default.FieldsToFind = JsonConvert.SerializeObject(Program.FieldsList);
            Settings.Default.Save();
        }

        private void listView2_MouseClick(object sender, MouseEventArgs e)
        {
            ListView.SelectedIndexCollection s = listView2.SelectedIndices;
            if (s.Count > 0)
            {
                _editingFieldIndex = s[0];
                FieldToFind f = Program.FieldsList[_editingFieldIndex.Value];
                txtName.Text = f.Name;
                txtRegexPattern.Text = f.Regex;
                numericUpDown1.Value = f.GroupNumber;
                cbRequired.Checked = f.Required;
                btnNewAdd.Text = "Save Changes";
                pnlNewField.Visible = true;
            }
        }

        private void btnNewRemove_Click(object sender, EventArgs e)
        {
            if (!_editingFieldIndex.HasValue)
            {
                pnlNewField.Visible = false;
            }
            else
            {
                Program.FieldsList.RemoveAt(_editingFieldIndex.Value);
                this.SetFieldsToFind();
                _editingFieldIndex = null;
                pnlNewField.Visible = false;
            }
        }
    }
}