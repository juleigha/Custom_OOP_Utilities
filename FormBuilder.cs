using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.IO;
using System.Globalization;

namespace Intranet_Forms.IntranetApp.Bases
{
    public partial class FormBuilder : FormBase
    {
        public Dictionary<string, FormField> Fields = new Dictionary<string, FormField>();
        public Dictionary<string, FormField> Data = new Dictionary<string, FormField>();
        public Dictionary<string, FormField> Consoles = new Dictionary<string, FormField>();
        public Dictionary<string, string> stringData = new Dictionary<string, string>();
        public Object Castable = null;
        public delegate void Submit(FormBuilder form);
        public delegate void SubmitCastable(object ob, FormBuilder form);
        public delegate void DisplayUpdate(FormBuilder form);
        private Button _submitbtn = new Button();
        private Submit _submitfn;
        private SubmitCastable _submitCastfn;
        private DisplayUpdate _displayfn;
        public EventHandler update;
        public bool oneControlPerRow = false;
        private void init(string title, Submit sfn, DisplayUpdate dfn)
        {
            InitializeComponent();
            this.Text = title;
            _submitfn = sfn;
            _displayfn = dfn;
            BttnField submitbtn = new BttnField("Submit", SubmitClick);
            _submitbtn = submitbtn.Button();
            _submitbtn.BackColor = Color.DarkBlue;
            _submitbtn.ForeColor = Color.White;
            _submitbtn.Dock = DockStyle.Bottom;
            _submitbtn.Size = new Size(Width, 35);
            this.Controls.Add(_submitbtn);
            this.update += groupBox_Paint;
        }
        public FormBuilder(string title, Submit sfn, DisplayUpdate dfn = null)
        {
            init(title, sfn, dfn);
        }
        public FormBuilder(string title, SubmitCastable sfn, object fields, FormField.validate valid = null, bool isValidating = false, DisplayUpdate dfn = null, bool allRequired = false)
        {
            init(title, null, dfn);
            Castable = fields;
            _submitCastfn = sfn;
            foreach (var f in (fields.GetType()).GetProperties())
            {
                var t = fields.GetType().GetProperty(f.Name).PropertyType.Name;
                var val = fields.GetType().GetProperty(f.Name).GetValue(fields) == null ? "" : fields.GetType().GetProperty(f.Name).GetValue(fields);
                var lbl = f.Name.Replace("_", " ");
                switch (t)
                {
                    case "Boolean":
                        AddField(f.Name, new CheckField(lbl, valid), true);
                        Fields[f.Name].linkedControl = new CheckBox() { Checked = (Boolean)val };
                        break;
                    case "DateTime":
                        AddField(f.Name, new DateField(lbl, "d", allRequired, valid), true);
                        Fields[f.Name].linkedControl = new TextBox() { Text = ((DateTime)val).Year == 1 ? "" : ((DateTime)val).ToString("MM/dd/yyyy") };
                        break;
                    case "String":
                        AddField(f.Name, new StringField(lbl, allRequired, valid), true);
                        Fields[f.Name].linkedControl = new TextBox() { Text = val.ToString() };
                        break;
                    case "Decimal":
                        AddField(f.Name, new NumberField(lbl, allRequired, true, valid), true);
                        Fields[f.Name].linkedControl = new TextBox() { Text = val.ToString() };
                        break;
                    case "Int32":
                        AddField(f.Name, new NumberField(lbl, allRequired, false, valid), true);
                        Fields[f.Name].linkedControl = new TextBox() { Text = val.ToString() };
                        break;
                    case "Byte[]":
                        AddField(f.Name, new FileField(lbl, "", allRequired, null), true);
                        Fields[f.Name].linkedControl = new TextBox() { Text = val.ToString() };
                        break;
                    case "Byte[][]":
                        AddField(f.Name, new ListField(lbl, allRequired, true), true);
                        Fields[f.Name].linkedControl = new TextBox() { Text = val.ToString() };
                        break;
                    default: break;
                }
                if (valid != null)
                {
                    Fields[f.Name].stringData.Add(val.ToString());
                }
            }
        }
        public void BuildControl()
        {
            foreach (var field in Fields.Values)
            {
                field.Build();
                if (oneControlPerRow) field.container.Size = new Size(this.Width, field.container.Height);
                this.groupBox.Controls.Add(field.container);
                if (field.linkedControl != null)
                {
                    switch (field.Type)
                    {
                        case "Checkbox":
                            ((CheckBox)field.Control()).Checked = ((CheckBox)field.linkedControl).Checked;
                            break;
                        case "Combo":
                            ((ComboBox)field.Control()).SelectedIndex = ((ComboBox)field.Control()).Items.IndexOf(field.linkedControl.Text);
                            break;
                        case "DataSetCombo":
                            ((ComboBox)field.Control()).SelectedItem = ((ComboBox)field.Control()).Items[((ComboBox)field.Control()).FindString(field.linkedControl.Text)];
                            break;
                        default:
                            field.Control().Text = field.linkedControl.Text;
                            break;
                    }
                }
            }
            foreach (var c in Consoles.Values)
            {
                c.ConsoleCtnrl().MinimumSize = new Size(340, 20);
                this.consoleView.Controls.Add(c.ConsoleCtnrl());
            }
        }
        public void Reset()
        {
            foreach (var field in Fields.Values)
            {
                field.Control().Text = "";
                if (field.Type == "AddableList") ((ListBox)field.Control()).Items.Clear();
            }
            foreach (var c in Consoles.Values)
            {
                c.lbl.Text = "";
            }
        }
        public FormField AddField(string n, FormField f, bool stringDisplay = false)
        {
            if (Fields.ContainsKey(n)) return Fields[n];
            Fields.Add(n, f);

            Fields[n].key = n;

            if (stringDisplay)
                Consoles.Add(n, new ConsoleField(f.name));

            return Fields[n];
        }
        public void AddDisplay(string n, FormField f)
        {
            if (Consoles.ContainsKey(n)) return;
            Consoles.Add(n, f);
        }
        public void RemoveField(string key)
        {
            if (Fields.ContainsKey(key))
                Fields.Remove(key);
            if (Consoles.ContainsKey(key))
                Consoles.Remove(key);
        }
        private void SubmitClick(object sender, EventArgs e)
        {
            //foreach (var f in Fields.Values.ToArray())
            //    f.isError = !f.isValid(f);

            if (Fields.Values.ToArray().Where(x => !x.isValid(x) && x.isRequired).ToArray().Length > 0)
            {
                MessageBox.Show($"{Fields.Values.ToArray().Where(x => !x.isValid(x)).ToArray()[0].key} is not valid");
                return;
            }
            if (Castable != null)
            {
                foreach (var f in Fields)
                {
                    var ok = Castable.GetType().GetProperty(f.Key);
                    if (ok == null)
                        continue;
                    var type = ok.PropertyType.Name;
                    if (!String.IsNullOrEmpty(f.Value.Text) || f.Value.Type == "Checkbox")
                        switch (type)
                        {
                            case "Boolean":
                                Castable.GetType().GetProperty(f.Key).SetValue(Castable, f.Value.isChecked());
                                break;
                            case "DateTime":
                                DateTime.TryParseExact(f.Value.Text, ((DateField)f.Value).format, null, DateTimeStyles.None, out var x);
                                Castable.GetType().GetProperty(f.Key).SetValue(Castable, x);
                                break;
                            case "Decimal":
                                Castable.GetType().GetProperty(f.Key).SetValue(Castable, Decimal.Parse(f.Value.Text));
                                break;
                            case "Int32":
                                Castable.GetType().GetProperty(f.Key).SetValue(Castable, Int32.Parse(f.Value.Text));
                                break;
                            case "Byte[]":
                                var ba = File.ReadAllBytes(f.Value.Text);
                                Castable.GetType().GetProperty(f.Key).SetValue(Castable, ba);
                                break;
                            case "Byte[][]":
                                byte[][] ba2 = new byte[][] { new byte[] { } };
                                foreach (var name in f.Value.stringData)
                                {
                                    ba2.Append(File.ReadAllBytes(name));
                                }
                                Castable.GetType().GetProperty(f.Key).SetValue(Castable, ba2);
                                break;
                            default:
                                Castable.GetType().GetProperty(f.Key).SetValue(Castable, f.Value.Text);
                                break;
                        }
                }
                _submitCastfn(Castable, this);
            }
            else
                _submitfn(this);
        }
        private void groupBox_Paint(object sender, EventArgs e)
        {

            foreach (var c in Consoles.Keys)
            {
                if (Fields.ContainsKey(c))
                    Consoles[c].Display(Fields[c]);
                else
                    Consoles[c].Display(Consoles[c].value, Consoles[c].isError);
            }
            if (_displayfn == null) return;
            else _displayfn(this);
        }
    }

    abstract public class FormField
    {

        public string name = "";
        public string key = "";
        public string Type = "";
        public string value;
        public delegate bool validate(FormField obj);
        public validate isValid;
        public Panel container = new Panel();
        public Label lbl = new Label();
        public Label errlbl = new Label();
        public ConsoleField consoleControl = null;
        public bool isError = false;
        public abstract Control Control();
        public List<string> stringData = new List<string>();
        public List<DataRow> tableData = new List<DataRow>();
        public bool isRequired = false;
        public string selectedValue = "";
        public string emessage = "";
        public virtual Button PrimaryBtn() { return null; }
        public virtual Button SecondaryBtn() { return null; }
        public virtual FlowLayoutPanel ConsoleCtnrl() { return null; }
        public virtual bool isChecked() { return false; }
        public virtual string ComboValue() { return ""; }
        public Control linkedControl = null;
        public string Text { get { return this.Control().Text; } }

        public void AddErrorLable()
        {
            errlbl.Text = $"{name} is not valid";
            errlbl.Name = $"textBoxErrLabel{name}";
            errlbl.MinimumSize = new Size(345, 20);
            errlbl.ForeColor = Color.Red;
            errlbl.Visible = false;
            errlbl.TabIndex = 2;
        }
        public ConsoleField AddConsoleCntrl()
        {
            consoleControl = new ConsoleField(name);
            return consoleControl;
        }
        public void Build()
        {
            container.AutoSize = true;
            container.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            container.MinimumSize = new Size(345, 20);
            container.Margin = new Padding(0);
            lbl.Margin = new Padding(0);
            errlbl.Margin = new Padding(0);
            lbl.Size = new Size(345, 20);
            var cnt = this.Control();
            cnt.Margin = new Padding(0);
            cnt.MinimumSize = new Size(340, 20);
            var px = 10;
            if (!String.IsNullOrEmpty(lbl.Text))
            {
                lbl.Location = new Point(0, px);
                px += lbl.Height + 5;
            }
            cnt.Location = new Point(0, px);
            px += cnt.Height + 5;
            var btn = PrimaryBtn();
            if (btn != null)
            {
                px += 5;
                btn.Margin = new Padding(0);
                btn.Padding = new Padding(0);
                btn.Location = new Point(0, px);
                btn.BackColor = Color.DarkBlue;
                btn.ForeColor = Color.White;
                container.Controls.Add(btn);
                var rbtn = SecondaryBtn();
                if (rbtn != null)
                {
                    double x = cnt.Width / 2;
                    rbtn.Margin = new Padding(0);
                    rbtn.Padding = new Padding(0);
                    rbtn.Location = new Point((Int32)Math.Round(x), px);
                    rbtn.BackColor = Color.LightBlue;
                    rbtn.ForeColor = Color.White;
                    container.Controls.Add(rbtn);
                }
                px += btn.Height + 2;
            }

            errlbl.Location = new Point(0, px);
            container.Controls.Add(cnt);
            container.Controls.Add(lbl);
            container.Controls.Add(errlbl);

        }
        public void ValueChanged(object sender, EventArgs e)
        {
            isError = isValid == null ? false : !isValid(this);
            errlbl.Visible = isError && isRequired;
            container.Update();
            ((FormBuilder)this.container.Parent.Parent).update?.Invoke(this.container.Parent, EventArgs.Empty);
        }
        public void Display(FormField val)
        {
            lbl.ForeColor = val.isError ? Color.Red : Color.Black;
            switch (val.Type)
            {
                case "Checkbox":
                    lbl.Text = val.isError ? val.emessage : val.isChecked() ? "TRUE" : "FALSE";
                    break;
                case "DataSetCombo":
                    lbl.Text = val.isError ? val.emessage : ((ComboBox)val.Control()).SelectedItem == null ? "" : ((DataRowView)((ComboBox)val.Control()).SelectedItem).Row[((ComboBox)val.Control()).ValueMember].ToString();
                    break;
                default:
                    lbl.Text = val.isError ? val.emessage : val.Control().Text;
                    break;
            }
        }
        public void Display(string val, bool error)
        {
            lbl.ForeColor = error ? Color.Red : Color.Black;
            lbl.Text = val;
        }
        //public static bool CastableDisplay(FormField f)
        //{
        //    var t = f.Type == "Checkbox" ? f.isChecked().ToString() : f.Control().Text.Trim();
        //    f.emessage = t;
        //    if (f.Type == "Checkbox" && f.isChecked() != ((CheckBox)f.linkedControl).Checked)
        //        return false;
        //    if (f.Type != "Checkbox" && f.linkedControl.Text != f.Text) return false;
        //    return true;
        //}
    }
    public class StringField : FormField
    {
        public TextBox Input = new TextBox();
        public StringField(string key, bool required = true, validate f = null)
        {
            init(key, required, f);
        }
        public StringField(string key, string[] options, bool required = true, validate f = null)
        {
            init(key, required, f);
            stringData = options.ToList();
        }
        public StringField(string key, DataRow[] options, bool required = true, validate f = null)
        {
            tableData = options.ToList();
            init(key, required, f);
        }
        public void init(string key, bool required, validate f)
        {
            Type = "String";
            isValid = f == null ? validateString : f;
            isRequired = required;
            name = key;
            lbl.Text = name;
            lbl.Name = $"textBoxLabel{name}";
            lbl.Size = new Size(100, 20);
            lbl.TabIndex = 2;
            Input.Name = $"textBox{name}";
            Input.Size = new Size(100, 20);
            Input.TabIndex = 2;
            Input.TextChanged += new EventHandler(ValueChanged);
            AddErrorLable();
        }
        public override Control Control()
        {
            return Input;
        }
        private bool validateString(FormField ob)
        {
            return !isRequired || (!String.IsNullOrEmpty(ob.Control().Text) && !String.IsNullOrWhiteSpace(ob.Control().Text));
        }

    }
    public class NumberField : FormField
    {
        public TextBox Input = new TextBox();
        private bool _isDecimal;
        public NumberField(string key, bool required = true, bool isDecimal = true, validate f = null)
        {
            Type = "String";
            isValid = f == null ? validateNumber : f;
            isRequired = required;
            _isDecimal = isDecimal;
            name = key;
            lbl.Text = name;
            lbl.Name = $"textBoxLabel{name}";
            lbl.Size = new Size(100, 20);
            lbl.TabIndex = 2;
            Input.Name = $"textBox{name}";
            Input.Size = new Size(100, 20);
            Input.TabIndex = 2;
            Input.TextChanged += new EventHandler(ValueChanged);
            AddErrorLable();
        }
        public override Control Control()
        {
            return Input;
        }
        private bool validateNumber(FormField ob)
        {
            emessage = $"{ob.name} Must be a Number";
            if (String.IsNullOrEmpty(ob.Text)) return false;
            if (_isDecimal)
                return Decimal.TryParse(ob.Text, out _);

            return Int32.TryParse(ob.Text, out _);
        }

    }
    public class ComboField : FormField
    {
        public ComboBox Input = new ComboBox();
        public override string ComboValue()
        {
            return ((DataRowView)Input.SelectedItem).Row[((ComboBox)Input).ValueMember].ToString();
        }
        public ComboField(string key, string[] items, bool required = true, validate f = null, bool canType = false)
        {
            Type = "Combo";
            Input.Items.AddRange(items);
            init(key, required, f, canType);
        }
        public ComboField(string key, DataTable items, string display, string value, bool required = true, validate f = null, bool canType = false)
        {
            BindingSource bs = new BindingSource();
            bs.DataSource = items;
            Input.BindingContext = new BindingContext();
            Input.DataBindings.Add(
            new System.Windows.Forms.Binding("DisplayMember", bs, display, true));
            Type = "DataSetCombo";
            Input.DataSource = items;
            Input.DisplayMember = display;
            Input.ValueMember = value;
            init(key, required, f, canType);
        }
        private void init(string key, bool required, validate f, bool canType)
        {
            isValid = f == null ? validateCombo : f;
            isRequired = required;
            name = key;
            lbl.Text = name;
            lbl.Name = $"comboBoxLabel{name}";
            lbl.Size = new Size(100, 20);
            lbl.TabIndex = 2;
            Input.Name = $"comboBox{name}";
            Input.FormattingEnabled = true;
            Input.Size = new Size(190, 21);
            Input.TabIndex = 0;
            Input.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
            Input.AutoCompleteSource = AutoCompleteSource.ListItems;
            Input.DropDownStyle = canType ? ComboBoxStyle.DropDown : ComboBoxStyle.DropDownList;
            AddErrorLable();
            Input.SelectedValueChanged += new EventHandler(change);
            Input.MouseWheel += (o, e) => ((HandledMouseEventArgs)e).Handled = true;
        }
        private void change(object sender, EventArgs e)
        {
            Input.Text = Input.SelectedItem == null ? "" : Input.Text;
            ValueChanged(sender, e);
        }
        public override Control Control()
        {
            return Input;
        }
        private bool validateCombo(FormField ob)
        {
            if (Input.SelectedValue == null && !Input.Items.Contains(Input.Text)) return false;
            if (Input.DataSource != null)
                return Input.SelectedValue.ToString() == ((DataRowView)Input.SelectedItem).Row[Input.ValueMember].ToString();
            else
                return Input.Items.Contains(Input.Text);
        }
    }
    public class ListField : FormField
    {
        public ListBox Input = new ListBox();
        public delegate void fileDrop(List<string> files);
        public fileDrop dropcb;
        private Button _browseBtn = null;
        private Button _removeBtn = null;
        private bool _fileDrop = false;
        public ListField(string key, bool required = false, bool fileDrop = false, fileDrop fd = null, validate f = null)
        {
            Type = "List";
            isValid = f == null && fileDrop ? validateFiles : f == null ? DontValidateFiles : f;
            name = key;
            isRequired = required;
            _fileDrop = fileDrop;
            lbl.Text = name;
            lbl.Name = $"listBoxLabel{name}";
            lbl.Size = new Size(100, 20);
            lbl.TabIndex = 2;
            Input.Name = $"listBox{name}";
            Input.Size = new Size(690, 95);
            Input.TabIndex = 0;
            //Input.FormattingEnabled = true;
            AddErrorLable();
            Input.SelectedIndexChanged += new EventHandler(ValueChanged);
            if (fileDrop)
            {
                Type = "AddableList";
                dropcb = fd;
                _browseBtn = new BttnField($"Add {name} Files", OpenFileDialog).Button();
                _removeBtn = new BttnField("Remove selected", RemoveFile).Button();
                container.AllowDrop = true;
                container.DragEnter += new DragEventHandler(on_DragEnter);
                container.DragDrop += new DragEventHandler(on_DragDrop);
            }
        }
        public override Control Control()
        {
            return Input;
        }
        public override Button PrimaryBtn()
        {
            return _browseBtn;
        }
        public override Button SecondaryBtn()
        {
            return _removeBtn;
        }
        public void on_DragDrop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            Input.Items.AddRange(files);
            stringData.AddRange(files);
            ValueChanged(this, EventArgs.Empty);
            if (dropcb != null) dropcb(stringData);
        }
        public void on_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Move;
        }
        public void OpenFileDialog(object sender, EventArgs e)
        {
            var browser = new OpenFileDialog();
            browser.Multiselect = true;
            browser.ShowDialog();
            Input.Items.AddRange(browser.FileNames);
            stringData.AddRange(browser.FileNames);
            ValueChanged(this, EventArgs.Empty);
            if (dropcb != null) dropcb(stringData);
        }
        public void RemoveFile(object sender, EventArgs e)
        {
            if (Input.SelectedItem != null)
            {
                stringData.Remove(Input.SelectedItem.ToString());
                Input.Items.RemoveAt(Input.SelectedIndex);
                ValueChanged(this, EventArgs.Empty);
            }
        }
        private bool validateFiles(FormField ob)
        {
            return _fileDrop ? stringData.Where(file => File.Exists(file)).Count() == stringData.Count : Input.Items.Contains(Input.SelectedValue);
        }
        private bool DontValidateFiles(FormField ob)
        {
            return true;
        }
    }
    public class FileField : FormField
    {
        public TextBox Input = new TextBox();
        public delegate void fileDrop(string file);
        public fileDrop dropcb;
        private Button _browseBtn = null;
        private string _docType = "";
        public FileField(string key, string docType, bool required = true, validate f = null, fileDrop fd = null)
        {
            Type = "File";
            _docType = docType;
            isValid = f == null ? validateFile : f;
            name = key;
            isRequired = required;
            lbl.Text = name;
            lbl.Name = $"FileBoxLabel{name}";
            lbl.Size = new Size(100, 20);
            lbl.TabIndex = 2;
            Input.Name = $"FileBox{name}";
            Input.Size = new Size(250, 30);
            Input.TabIndex = 0;
            Input.TextChanged += new System.EventHandler(ValueChanged);
            AddErrorLable();
            var btn = new BttnField($"Add {name} File", OpenFileDialog);
            _browseBtn = btn.Button();
            container.AllowDrop = true;
            container.DragEnter += new DragEventHandler(on_DragEnter);
            container.DragDrop += new DragEventHandler(on_DragDrop);
        }
        public override Control Control()
        {
            return Input;
        }
        public override Button PrimaryBtn()
        {
            return _browseBtn;
        }
        public void on_DragDrop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            Input.Text = files[0];
            ValueChanged(this, EventArgs.Empty);
            if (dropcb != null) dropcb(Input.Text);
        }
        public void on_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Move;
        }
        public void OpenFileDialog(object sender, EventArgs e)
        {
            var browser = new OpenFileDialog();
            browser.Multiselect = false;
            if (!String.IsNullOrEmpty(_docType)) browser.Filter = _docType;
            browser.ShowDialog();
            Input.Text = browser.FileName;
            ValueChanged(this, EventArgs.Empty);
            if (dropcb != null) dropcb(Input.Text);
        }
        private bool validateFile(FormField ob)
        {
            return File.Exists(ob.Control().Text);
        }
    }
    public class CheckField : FormField
    {
        public CheckBox Input = new CheckBox();
        public override Control Control()
        {
            return Input;
        }
        public override bool isChecked()
        {
            return Input.Checked;
        }
        private bool validateCheck(FormField ob)
        {
            return true;
        }
        public CheckField(string key, validate fn = null)
        {
            isRequired = false;
            Type = "Checkbox";
            name = key;
            Input.Name = $"CheckBox{name}";
            Input.TabIndex = 0;
            Input.AutoSize = true;
            Input.Text = name;
            Input.Padding = new Padding(0, 20, 0, 0);
            Input.UseVisualStyleBackColor = true;
            Input.CheckedChanged += new EventHandler(ValueChanged);
            isValid = fn == null ? validateCheck : fn;
        }
    }
    public class BttnField : FormField
    {
        private Button Input = new Button();
        public delegate void BttnClick(object sender, EventArgs e);
        public override Control Control()
        {
            return Input;
        }
        public Button Button()
        {
            return Input;
        }
        public BttnField(string key, BttnClick click)
        {
            Type = "Button";
            name = key;
            Input.Name = $"button{name}";
            Input.TabIndex = 0;
            Input.Text = name;
            Input.UseVisualStyleBackColor = true;
            Input.AutoSize = true;
            Input.FlatStyle = FlatStyle.Popup;
            Input.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            Input.Click += new EventHandler(click);
        }

    }
    public class DateField : FormField
    {
        public TextBox Input = new TextBox();
        public string format;
        public DateField(string key, string dformat, bool required = true, validate f = null)
        {
            Type = "Date";
            format = dformat;
            isValid = f == null ? validateDate : f;
            isRequired = required;
            name = key;
            lbl.Text = name;
            lbl.Name = $"textBoxLabel{name}";
            lbl.Size = new Size(100, 20);
            lbl.TabIndex = 2;
            Input.Name = $"textBox{name}";
            Input.Size = new Size(100, 20);
            Input.TabIndex = 2;
            Input.TextChanged += new EventHandler(ValueChanged);
            AddErrorLable();
        }
        public override Control Control()
        {
            return Input;
        }
        private bool validateDate(FormField ob)
        {
            DateTime r;
            ob.emessage = $"Format is {format}";
            if (new DateTime(2000, 1, 1).ToString(format).Length - ob.Text.Length > 2) return false;
            ob.emessage = $"Date is invalid";
            if (!DateTime.TryParseExact(ob.Text, format, null, DateTimeStyles.None, out r)) return false;
            return true;
        }

    }
    public class ConsoleField : FormField
    {
        private Label Input = new Label();
        private FlowLayoutPanel cnsl = new FlowLayoutPanel();
        public delegate void BttnClick(object sender, EventArgs e);
        public override FlowLayoutPanel ConsoleCtnrl()
        {
            return cnsl;
        }

        public override Control Control()
        {
            return Input;
        }
        public ConsoleField(string key)
        {
            Type = "Console";
            name = key;
            Input.Name = $"Console{name}";
            Input.Size = new Size(100, 20);
            lbl.Size = new Size(100, 20);
            lbl.Font = new Font("Tahoma", 12, FontStyle.Bold);
            Input.TabIndex = 0;
            Input.Text = name;
            Input.Location = new Point(0, 0);
            lbl.Location = new Point(Input.Width, 0);
            cnsl.AutoSize = true;
            cnsl.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            lbl.AutoSize = true;
            Input.AutoSize = true;
            cnsl.Controls.Add(Input);
            cnsl.Controls.Add(lbl);
        }

    }
}
