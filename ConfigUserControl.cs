using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Text.RegularExpressions;
using System.IO;
using Newtonsoft.Json;

namespace ReceiveMorphPlugin
{
    public partial class ConfigUserControl : UserControl
    {
        public static int deviceCount = 53;
        public List<string>[] modelsNames = new List<string>[deviceCount];
        public List<string>[] boneNames = new List<string>[deviceCount];
        public List<string>[] morphNames = new List<string>[deviceCount];

        public class JsonData
        {
            public int[] modelIndex = new int[deviceCount];
            //public int[] boneIndex = new int[deviceCount];
            public int[] morphIndex = new int[deviceCount];
        }

        public JsonData jsonData = new JsonData();
    
        public float[] morphWeight = new float[deviceCount];

        // HMDとコントローラーのズレをオフセットで補正する
        public float headAxisOffset = -12f;
        public float handAxisOffset = -3.5f;

        // 移動倍率
        public float moveMagnify = 10f;
        // 回転倍率
        public float rotationMagnify = 1f;

        public bool isRecord = false;

        private string filePath = "";
        private string defaultFileName = "morph_settings.json";
        // vroid2pmx用モーフ名
        private string[] morphName = {
            "Basis",
            "ひそめる",
            "真面目左",
            "真面目右",
            "はんっ左",
            "はんっ右",
            "目上左",
            "目上右",
            "目下左",
            "目下右",
            "目頭広左",
            "目頭広右",
            "目尻広右",
            "目尻広左",
            "まばたき左",
            "まばたき右",
            "にんまり左",
            "にんまり右",
            "びっくり2左",
            "びっくり2右",
            "ぷくー",
            "にひひ左",
            "にひひ右",
            "キリッ2左",
            "キリッ2右",
            "あああ",
            "顎左",
            "顎右",
            "顎前",
            "んむー",
            "うー",
            "口左",
            "口右",
            "上唇んむー",
            "下唇んむー",
            "上唇むむ",
            "下唇むむ",
            "mouseClose",
            "にやり2左",
            "にやり2右",
            "ちっ左",
            "ちっ右",
            "口幅広左",
            "口幅広右",
            "にひ左",
            "にひ右",
            "むっ左",
            "むっ右",
            "薄笑い左",
            "薄笑い右",
            "ぎりっ左",
            "ぎりっ右",
            "べー"
        };
        public ConfigUserControl()
        {
            InitializeComponent();
            InitNames();
            InitIndexes();
        }

        public void InitNames()
        {
            for (int i = 0; i < deviceCount; i++)
            {
                modelsNames[i] = new List<string>();
                boneNames[i] = new List<string>();
                morphNames[i] = new List<string>();
            }
        }

        public void InitMorphes()
        {
            for (int i = 0; i < deviceCount; i++)
            {
                morphNames[i] = new List<string>();
            }
        }

        public void InitIndexes()
        {
            for (int i = 0; i < deviceCount; i++)
            {
                jsonData.modelIndex[i] = -1;
                //jsonData.boneIndex[i] = -1;
                jsonData.morphIndex[i] = -1;
                //morphWeight[i] = -1;
            }
        }
        
        /// <summary>
        /// コンボボックスからインデックス番号を取得する
        /// </summary>
        /// <param name="comboBox"></param>
        /// <returns></returns>
        private int GetIndexNumberFromComboBox(ComboBox comboBox)
        {
            int result = -1;
            string str = Regex.Replace(comboBox.Name, @"[^0-9]", "");
            int.TryParse(str, out result);
            return result;
        }

        public void ReneualComboBoxName(ComboBox comboBox, List<string>[]names)
        {
            comboBox.Items.Clear();

            foreach(string comboName in names[GetIndexNumberFromComboBox(comboBox)])
            {
                comboBox.Items.Add(comboName);
            }
        }

        private void ModelComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            ComboBox comboBox = (ComboBox)sender;
            jsonData.modelIndex[GetIndexNumberFromComboBox(comboBox)] = comboBox.SelectedIndex;
        }

        private void ModelComboBox_OnClick(object sender, EventArgs e)
        {
            ComboBox comboBox = (ComboBox)sender;
            ReneualComboBoxName(comboBox, modelsNames);
        }

        private void BoneComboBox_OnClick(object sender, EventArgs e)
        {
            ComboBox comboBox = (ComboBox)sender;
            ReneualComboBoxName(comboBox, boneNames);
        }

        private void MorphComboBox_OnClick(object sender, EventArgs e)
        {
            ComboBox comboBox = (ComboBox)sender;
            ReneualComboBoxName(comboBox, morphNames);
        }

        //private void BoneComboBox_SelectedIndexChanged(object sender, EventArgs e)
        //{
        //    ComboBox comboBox = (ComboBox)sender;
        //    jsonData.boneIndex[GetIndexNumberFromComboBox(comboBox)] = comboBox.SelectedIndex;
        //}

        private void MorphComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            ComboBox comboBox = (ComboBox)sender;
            jsonData.morphIndex[GetIndexNumberFromComboBox(comboBox)] = comboBox.SelectedIndex;
        }

        private void moveMagnifyTextBox_TextChanged(object sender, EventArgs e)
        {
            TextBox textBox = (TextBox)sender;
            Single.TryParse(textBox.Text, out moveMagnify);
        }

        private void RecordToggleButton_CheckedChanged(object sender, EventArgs e)
        {
            CheckBox checkBox = (CheckBox)sender;
            isRecord = checkBox.Checked;
        }

        private void RotationMagnifyTextBox_TextChanged(object sender, EventArgs e)
        {
            TextBox textBox = (TextBox)sender;
            Single.TryParse(textBox.Text, out rotationMagnify);
        }

        private void SaveJsonButton_Click(object sender, EventArgs e)
        {
            var saveFileDialog = new SaveFileDialog();
            
            if (filePath != null)
            {
                saveFileDialog.InitialDirectory = filePath;
            }

            saveFileDialog.CheckFileExists = false;
            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                filePath = Path.GetDirectoryName(saveFileDialog.FileName);
                var jsonString = JsonConvert.SerializeObject(jsonData);
                File.WriteAllText(saveFileDialog.FileName, jsonString);
            }
        }

        private void OpenJsonButton_Click(object sender, EventArgs e)
        {
            var openFieldDialog = new OpenFileDialog();

            if (filePath != null)
            {
                openFieldDialog.InitialDirectory = filePath;
            }

            if (openFieldDialog.ShowDialog() == DialogResult.OK)
            {
                filePath = Path.GetDirectoryName(openFieldDialog.FileName);
                var json = File.ReadAllText(openFieldDialog.FileName);
                jsonData = JsonConvert.DeserializeObject<JsonData>(json);
            }
            else
            {
                return;
            }

            for (int i = 0; i < deviceCount; i++)
            {
                //if(jsonData.boneIndex[i] == -1 || jsonData.morphIndex[i] == -1)
                if (jsonData.morphIndex[i] == -1)
                {
                    continue;
                }

                ////DisplayLog("BoneComboBox" + i.ToString());
                //var controll = Controls.Find("MorphComboBox" + i.ToString(), true);
                //var combo = (ComboBox)controll[0];
                //ReneualComboBoxName(combo, boneNames);
                ////combo.Select(jsonData.boneIndex[i], 1);
                ////combo.SelectedIndex = jsonData.boneIndex[i];

                var controll = Controls.Find("MorphComboBox" + i.ToString(), true);
                var combo = (ComboBox)controll[0];
                ReneualComboBoxName(combo, morphNames);
                //combo.Select(jsonData.trackerIndex[i], 1);
                combo.SelectedIndex = jsonData.morphIndex[i];
            }
        }

        private void DisplayLog(string text)
        {
            LogTextBox.Text += text + "\r\n";
            LogTextBox.SelectionStart = LogTextBox.Text.Length;
            LogTextBox.ScrollToCaret();
        }

        private void AutoSetting_Click(object sender, EventArgs e)
        {
            bool okFlag = false;
            for (int i = 0; i < deviceCount; i++)
            {

                var controll = Controls.Find("TrackerComboBox" + i.ToString(), true);
                var combo = (ComboBox)controll[0];
                //combo.Select(jsonData.trackerIndex[i], 1);
                ReneualComboBoxName(combo, morphNames);
                combo.SelectedIndex = i;

                var MorphControll = Controls.Find("MorphComboBox" + i.ToString(), true);
                var MorphCombo = (ComboBox)MorphControll[0];
                ReneualComboBoxName(MorphCombo, boneNames);

                for (int j = 0; j < boneNames.Length; j++)
                {
                    MorphCombo.SelectedIndex = j;
                    if (String.Compare(morphName[i], MorphCombo.Text) == 0)
                    {
                        okFlag = true;
                        break;
                    }
                }

                if(okFlag == true)
                {
                    okFlag = false;
                }
                else
                {
                    MorphCombo.SelectedIndex = -1;
                }

            }
        }
    }
}
