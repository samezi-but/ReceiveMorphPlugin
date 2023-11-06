using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using MikuMikuPlugin;
using DxMath;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.IO;
using System.Threading;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace ReceiveMorphPlugin
{
    public class ReceiveMPlugin : IResidentPlugin, IHaveUserControl
    {
        /// <summary>
        /// ユーザーコントロール
        /// </summary>
        private ConfigUserControl configUserControl;

        /// <summary>
        /// ローカルで保存している時間
        /// </summary>
        private float time;

        /// <summary>
        /// このプラグインのGUID
        /// </summary>
        public Guid GUID
        {
            get { return new Guid("4AADA4D2-BE77-4B92-9C2A-E2F9F90B9E83"); }
        }

        /// <summary>
        /// メインフォーム
        /// MikuMikuMoving側から与えられます。
        /// ダイアログ表示やメッセージ表示に使用してください。
        /// </summary>
        public IWin32Window ApplicationForm { get; set; }

        /// <summary>
        /// シーンオブジェクト
        /// MikuMikuMoving側から与えられます。
        /// MikuMikuMovingのモデルやアクセサリといったオブジェクトにアクセスできます。
        /// </summary>
        public Scene Scene { get; set; }

        /// <summary>
        /// プラグインの名前や作者名、プラグインの説明
        /// </summary>
        public string Description
        {
            get { return "Receive Morph Information Plugin by samezi"; }
        }

        /// <summary>
        /// ボタンに表示するアイコン画像(32x32)
        /// nullだとデフォルト画像が表示されます。
        /// </summary>
        public Image Image
        {
            get { return null; }
            //get { return Properties.Resources.svrcp_icon_32; }
        }

        /// <summary>
        /// 中コマンドバーに表示するアイコン画像(20x20)
        /// nullだとデフォルト画像が表示されます。
        /// </summary>
        public Image SmallImage
        {
            get { return null; }
            //get { return Properties.Resources.svrcp_icon_20; }
        }

        /// <summary>
        /// ボタンに表示するテキスト
        /// 日本語環境で表示されるテキストです。改行する場合は Environment.NewLineを使用してください。
        /// </summary>
        public string Text
        {
            get { return "モーフ情報受信" + Environment.NewLine + "プラグイン"; }
        }

        /// <summary>
        /// ボタンに表示する英語テキスト
        /// 日本以外の環境で表示されるテキストです。
        /// </summary>
        public string EnglishText
        {
            get { return "ReceiveMorph" + Environment.NewLine + "Plugin"; }
        }

        /// <summary>
        /// Initialize
        /// プラグイン作成時に1度だけ呼ばれます。
        /// </summary>
        public void Initialize()
        {
            time = 0;

        }
        private UdpClient udp;
        private Thread thread;
        //List<GameObject> objectArray;
        // broadcast address
        public bool gameStartWithConnect = true;
        public string iOS_IPAddress = "192.168.1.6";
        private UdpClient client;
        private bool StartFlag = true;

        //object names
        public string faceObjectGroupName = "";
        public string headBoneName = "";
        public string rightEyeBoneName = "";
        public string leftEyeBoneName = "";
        public string headPositionObjectName = "";

        private string messageString = "";
        public int LOCAL_PORT = 49983;

        // 瞬きの値
        float eyeBlinkValue = 0f;
        // 有効化フラグ
        bool enabledFlag = false;
        // デバイスカウント
        public readonly int deviceCount = 53;
        // モデルカウント
        private int modelCount = 0;
        // モデルインデックス
        private int modelIndex = -1;
        // 録画時間
        public float fTotalElapsedTime = 0f;
        // 顔の部品名
        private string[] faceParts = {
            "Basis",
            "browInnerUp",
            "browDownLeft",
            "browDownRight",
            "browOuterUpLeft",
            "browOuterUpRight",
            "eyeLookUpLeft",
            "eyeLookUpRight",
            "eyeLookDownLeft",
            "eyeLookDownRight",
            "eyeLookInLeft",
            "eyeLookInRight",
            "eyeLookOutLeft",
            "eyeLookOutRight",
            "eyeBlinkLeft",
            "eyeBlinkRight",
            "eyeSquintLeft",
            "eyeSquintRight",
            "eyeWideLeft",
            "eyeWideRight",
            "cheekPuff",
            "cheekSquintLeft",
            "cheekSquintRight",
            "noseSneerLeft",
            "noseSneerRight",
            "jawOpen",
            "jawForward",
            "jawLeft",
            "jawRight",
            "mouthFunnel",
            "mouthPucker",
            "mouthLeft",
            "mouthRight",
            "mouthRollUpper",
            "mouthRollLower",
            "mouthShrugUpper",
            "mouthShrugLower",
            "mouthClose",
            "mouthSmileLeft",
            "mouthSmileRight",
            "mouthFrownLeft",
            "mouthFrownRight",
            "mouthDimpleLeft",
            "mouthDimpleRight",
            "mouthUpperUpLeft",
            "mouthUpperUpRight",
            "mouthLowerDownLeft",
            "mouthLowerDownRight",
            "mouthPressLeft",
            "mouthPressRight",
            "mouthStretchLeft",
            "mouthStretchRight",
            "tongueOut"
        };


        // 顔のモーフ
        private float[] faceMorphList = new float[53];
        // ボーンのクォータニオンを格納する
        private Quaternion[] boneQuaternion = new Quaternion[3];
        // 頭のクォータニオンのキャッシュ
        Quaternion headCacheQuart = new Quaternion();
        // 頭のクォータニオンのInvert
        Quaternion headInvertQuart = new Quaternion();

        /// <summary>
        /// Enabled
        /// プラグインが有効化されたときに呼び出されます。
        /// </summary>
        public void Enabled()
        {
            //Recieve udp from iOS
            CreateUdpServer();
            //Send to iOS
            Connect_to_iOS_App();
            enabledFlag = true;

        }

        /// <summary>
        /// 
        /// </summary>
        private void CreateUdpServer()
        {
            udp = new UdpClient(LOCAL_PORT);
            //udp.Close();
            udp.Client.ReceiveTimeout = 5000;

            thread = new Thread(new ThreadStart(ThreadMethod));
            DisplayLog("udp準備");
            thread.Start();
        }

        private void Connect_to_iOS_App()
        {
            //iFacialMocap
            SendMessage_to_iOSapp("iFacialMocap_sahuasouryya9218sauhuiayeta91555dy3719", 49983);
            DisplayLog("iFacalMocap");

            //Facemotion3d
            //SendMessage_to_iOSapp("FACEMOTION3D_OtherStreaming", 49993);
            //DisplayLog("FaceMotion3D");
        }

        private void StopStreaming_iOS_App()
        {
            SendMessage_to_iOSapp("StopStreaming_FACEMOTION3D", 49993);
            DisplayLog("Stop Streaming");
        }

        //iOSアプリに通信開始のメッセージを送信
        //Send a message to the iOS application to start streaming
        private void SendMessage_to_iOSapp(string sendMessage, int send_port)
        {
            try
            {
                client = new UdpClient();
                client.Connect(iOS_IPAddress, send_port);
                byte[] dgram = Encoding.UTF8.GetBytes(sendMessage);
                client.Send(dgram, dgram.Length);
                client.Send(dgram, dgram.Length);
                client.Send(dgram, dgram.Length);
                client.Send(dgram, dgram.Length);
                client.Send(dgram, dgram.Length);
            }
            catch {
                DisplayLog("Exception Log");
            }
        }

        public void MorphsComboboxUpdate()
        {
            for (int i = 0; i < deviceCount; i++)
            {
                if (configUserControl.jsonData.modelIndex[0] >= 0)
                {
                    MorphCollection morphs = Scene.Models[configUserControl.jsonData.modelIndex[0]].Morphs;
                    foreach (var bone in morphs)
                    {
                        configUserControl.morphNames[i].Add(bone.DisplayName);
                    }
                }
            }
        }

        private bool firstRecordFrag = true;
        private float frameCount = 0f;
        /// <summary>
        /// 更新
        /// 毎フレームごとに呼び出されます。
        /// </summary>
        /// <param name="Frame">現在のフレーム番号</param>
        /// <param name="ElapsedTime">前フレームからの差分実時間(秒)</param>
        public void Update(float Frame, float ElapsedTime)
        {

            // モデル数が変わったとき
            if (modelCount != Scene.Models.Count)
            {
                configUserControl.InitNames();
                // モデルを追加
                foreach (Model model in Scene.Models)
                {
                    configUserControl.modelsNames[0].Add(model.DisplayName);
                    modelCount++;
                }
            }

            // モデル切替のタイミングでコンボボックスの中身を更新する
            if(modelIndex != configUserControl.jsonData.modelIndex[0])
            {
                configUserControl.InitMorphes();
                MorphsComboboxUpdate();
                modelIndex = configUserControl.jsonData.modelIndex[0];
            }

            if (!enabledFlag) return;

            if(configUserControl.isRecord == true && ElapsedTime > 0.03333)
            {
                if(firstRecordFrag == true)
                {
                    frameCount = 0f;
                    firstRecordFrag = false;
                }
                frameCount++;
            }
            else
            {
                firstRecordFrag = true;
            }
            try
            {
                var strArray1 = this.messageString.Split(new Char[] { '=' });
                if (strArray1.Length >= 2)
                {
                    //DisplayLog("strArray1.Length >= 2");
                    //blendShapes
                    var index = 0;
                    foreach (string message in strArray1[0].Split(new Char[] { '|' }))
                    {
                        var strArray2 = new string[3];
                        if (message.Contains("&"))
                        {
                            strArray2 = message.Split('&');
                        }
                        else
                        {
                            strArray2 = message.Split('-');
                        }

                        if (strArray2.Length == 2)
                        {
                            var mappedShapeName = strArray2[0].Replace("_R", "Right").Replace("_L", "Left");
                            var weight = float.Parse(strArray2[1]);

                            for (int i = 0; i < faceParts.Length; i++)
                            {
                                if (mappedShapeName.Equals(faceParts[i]))
                                {
                                    faceMorphList[i] = weight / 100f;
                                    if (mappedShapeName.Equals("eyeBlinkLeft"))
                                    {
                                        eyeBlinkValue = faceMorphList[i];
                                    }
                                }
                            }
                            index++;
                        }
                    }

                    foreach (string message in strArray1[1].Split('|'))
                    {
                        var strArray2 = message.Split('#');

                        if (strArray2.Length == 2)
                        {
                            float horizontalMagnify = 0.002f;
                            float verticalMagnify = 0.005f;
                            float headMagnify = 0.01f;
                            var commaList = strArray2[1].Split(',');
                            if (strArray2[0] == "head")
                            {
                                //DisplayLog("head move");
                                boneQuaternion[0] = Quaternion.RotationYawPitchRoll(
                                    -float.Parse(commaList[1]) * headMagnify,
                                    -float.Parse(commaList[0]) * headMagnify,
                                    float.Parse(commaList[2]) * headMagnify);
                                headCacheQuart = boneQuaternion[0];
                                headInvertQuart = boneQuaternion[0];
                                headInvertQuart.Invert();
                            }
                            else if (strArray2[0] == "rightEye" && eyeBlinkValue < 0.02f)
                            {
                                boneQuaternion[1] = Quaternion.RotationYawPitchRoll(
                                    -float.Parse(commaList[1]) * horizontalMagnify,
                                    -float.Parse(commaList[0]) * verticalMagnify,
                                    float.Parse(commaList[2]) * horizontalMagnify)
                                    + Quaternion.RotationYawPitchRoll(0f, 0f, float.Parse(commaList[2]) * horizontalMagnify)
                                    ;
                            }
                            else if (strArray2[0] == "leftEye" && eyeBlinkValue < 0.02f)
                            {
                                boneQuaternion[2] = Quaternion.RotationYawPitchRoll(
                                    -float.Parse(commaList[1]) * horizontalMagnify,
                                    -float.Parse(commaList[0]) * verticalMagnify,
                                    float.Parse(commaList[2]) * horizontalMagnify)
                                    + Quaternion.RotationYawPitchRoll(0f, 0f, float.Parse(commaList[2]) * horizontalMagnify)
                                    ;
                            }
                        }
                    }
                }
            }
            catch
            { }

            // 条件が整ったらモデルを動かす
            if (configUserControl.jsonData.modelIndex[0] != -1)
            {
                BoneCollection bones = Scene.Models[configUserControl.jsonData.modelIndex[0]].Bones;

                MotionData motionData;
                for (int i = 0; i < bones.Count; i++)
                {
                    if (bones[i].Name == "頭")
                    {
                        motionData = Scene.Models[configUserControl.jsonData.modelIndex[0]].Bones[i].CurrentLocalMotion;
                        motionData.Rotation = boneQuaternion[0];
                        Scene.Models[configUserControl.jsonData.modelIndex[0]].Bones[i].CurrentLocalMotion = motionData;

                        if (configUserControl.isRecord == true)
                        {
                            RecordBone(Scene.Models[configUserControl.jsonData.modelIndex[0]].Bones[i], ElapsedTime, frameCount);
                        }
                    }
                    else if (bones[i].Name == "右目")
                    {
                        motionData = Scene.Models[configUserControl.jsonData.modelIndex[0]].Bones[i].CurrentLocalMotion;
                        motionData.Rotation = boneQuaternion[1] //* headInvertQuart
                            ;
                        Scene.Models[configUserControl.jsonData.modelIndex[0]].Bones[i].CurrentLocalMotion = motionData;

                        if (configUserControl.isRecord == true)
                        {
                            RecordBone(Scene.Models[configUserControl.jsonData.modelIndex[0]].Bones[i], ElapsedTime, frameCount);
                        }
                    }
                    else if (bones[i].Name == "左目")
                    {
                        motionData = Scene.Models[configUserControl.jsonData.modelIndex[0]].Bones[i].CurrentLocalMotion;
                        motionData.Rotation = boneQuaternion[2] //* headInvertQuart
                            ;
                        Scene.Models[configUserControl.jsonData.modelIndex[0]].Bones[i].CurrentLocalMotion = motionData;

                        if (configUserControl.isRecord == true)
                        {
                            RecordBone(Scene.Models[configUserControl.jsonData.modelIndex[0]].Bones[i], ElapsedTime, frameCount);
                        }
                    }
                }
            }
            for (int i = 0; i < deviceCount; i++)
            {
                // 条件が整ったらモデルを動かす
                if (configUserControl.jsonData.modelIndex[0] != -1 &&
                    configUserControl.jsonData.morphIndex[i] != -1
                    )
                {
                    Scene.Models[configUserControl.jsonData.modelIndex[0]].Morphs[configUserControl.jsonData.morphIndex[i]].CurrentWeight =
                        faceMorphList[i];
                    //DisplayLog("Morph Index is :" + configUserControl.jsonData.morphIndex[i]);
                    //DisplayLog("Index is :" + i);
                    //DisplayLog("Weight is :" + Scene.Models[configUserControl.jsonData.modelIndex[0]].Morphs[configUserControl.jsonData.morphIndex[i]].CurrentWeight);

                    if (configUserControl.isRecord == true)
                    {
                        RecordMorph(Scene.Models[configUserControl.jsonData.modelIndex[0]].Morphs[configUserControl.jsonData.morphIndex[i]], ElapsedTime, frameCount);
                    }
                }
            }
            if (configUserControl.isRecord == true)
            {
                fTotalElapsedTime += ElapsedTime;
            }
            else
            {
                fTotalElapsedTime = 0;
            }
        }

        private void RecordBone(Bone bone, float ElapsedTime, float frame)
        {
            //long num = (long)(fTotalElapsedTime * 100f / Scene.KeyFramePerSec);
            long num = (long)frame;
            MotionFrameData motionFrameData = new MotionFrameData();
            motionFrameData.FrameNumber = num;
            motionFrameData.Position = bone.CurrentLocalMotion.Move;
            motionFrameData.Quaternion = bone.CurrentLocalMotion.Rotation;
            bone.Layers[0].Frames.AddKeyFrame(motionFrameData);
        }

        private void RecordMorph(Morph morph, float ElapsedTime, float frame)
        {
            //long num = (long)(fTotalElapsedTime * 100f / Scene.KeyFramePerSec);
            long num = (long)frame;
            MorphFrameData morphFrameData = new MorphFrameData(num, 0.0f);
            morphFrameData.FrameNumber = num;
            morphFrameData.Weight = morph.CurrentWeight;
            morph.Frames.AddKeyFrame(morphFrameData);
        }

        /// <summary>
        /// Disabled
        /// プラグインが無効化されたときに呼び出されます。
        /// </summary>
        public void Disabled()
        {
            enabledFlag = false;
            Stop();
        }

        /// <summary>
        /// プラグイン破棄処理
        /// もし解放しないといけないオブジェクトがある場合は、ここで解放してください。
        /// </summary>
        public void Dispose()
        {
            Stop();
        }

        void ThreadMethod()
        {
            //Process once every 5ms
            long next = DateTime.Now.Ticks + 50000;
            long now;
            while (true)
            {
                try
                {
                    //DisplayLog("Wait Receive");
                    IPEndPoint remoteEP = null;

                    byte[] data = udp.Receive(ref remoteEP);
                    //DisplayLog("data:" + data.ToString());
                    this.messageString = Encoding.ASCII.GetString(data);
                    if (messageString.Length > 0)
                    {
                        //DisplayLog(messageString);
                    }
                }
                catch (Exception e)
                {
                    //DisplayLog(e.Message);
                }
                do
                {
                    now = DateTime.Now.Ticks;
                }
                while (now < next);
                next += 50000;
            }
        }

        public string GetMessageString()
        {
            return this.messageString;
        }

        void OnApplicationQuit()
        {
            thread.Abort();
        }

        public void StopUDP()
        {
            StopStreaming_iOS_App();
            if (udp != null)
            {
                udp.Close();
                thread.Abort();
            }
        }

        void Stop()
        {
            try
            {
                StopUDP();
            }
            catch (IOException)
            {
            }
        }


        private void DisplayLog(string text)
        {
            if (configUserControl != null && configUserControl.LogTextBox.Visible ==true)
            {
                configUserControl.LogTextBox.Text += text + "\r\n";
                configUserControl.LogTextBox.SelectionStart = configUserControl.LogTextBox.Text.Length;
                configUserControl.LogTextBox.ScrollToCaret();
            }
        }

        ////////////////////////////////////////////////////////////////////
        // ユーザーコントロール用プロパティ
        ////////////////////////////////////////////////////////////////////
        public UserControl CreateControl()
        {
            configUserControl = new ConfigUserControl();
            return configUserControl;
        }
    }
}
