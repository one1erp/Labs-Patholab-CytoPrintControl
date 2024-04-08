using LSExtensionWindowLib;
using LSSERVICEPROVIDERLib;
using Patholab_Common;
using Patholab_DAL_V1;
using Patholab_XmlService;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Oracle.DataAccess.Client;
using System.Configuration;
using System.Diagnostics;
using UserControl = System.Windows.Forms.UserControl;
using System.Timers;
using System.Threading;
using System.Threading.Tasks;


namespace CytoPrintControl
{


    [ComVisible(true)]
    [ProgId("CytoPrintControl.CytoPrintControlCls")]
    public partial class CytoPrintControlCls : UserControl, IExtensionWindow
    {

        #region Private fields

        private INautilusProcessXML xmlProcessor;
        private IExtensionWindowSite2 _ntlsSite;
        private INautilusServiceProvider sp;
        private INautilusDBConnection _ntlsCon;

        private DataLayer dal;
        public bool DEBUG;
        private string mboxHeader = "מסך אימות דגימה -  Nautilus";







        #endregion

        #region Ctor

        public CytoPrintControlCls()
        {
            InitializeComponent();
            //BackColor = Color.FromName("Control");
            this.Dock = DockStyle.Fill;
            label2.Visible = false;
            labelMisBdika.Text = "";
            labelResult.ForeColor = Color.Red;
        }

        #endregion



        #region Implementation of IExtensionWindow

        public bool CloseQuery()
        {
            if (dal != null) dal.Close();
            this.Dispose();
            return true;
        }

        public void Internationalise()
        {
        }

        public void SetSite(object site)
        {
            _ntlsSite = (IExtensionWindowSite2)site;

            _ntlsSite.SetWindowInternalName("CytoPrintControl");
            _ntlsSite.SetWindowRegistryName("CytoPrintControl");
            _ntlsSite.SetWindowTitle("הדפסת מדבקות T5");
            labelResult.Visible = false;
        }




        public void PreDisplay()
        {

            xmlProcessor = Utils.GetXmlProcessor(sp);

            Utils.GetNautilusUser(sp);

            InitializeData();

        }

        public WindowButtonsType GetButtons()
        {
            return LSExtensionWindowLib.WindowButtonsType.windowButtonsNone;
        }

        public bool SaveData()
        {
            return false;
        }

        public void SetServiceProvider(object serviceProvider)
        {
            sp = serviceProvider as NautilusServiceProvider;
            _ntlsCon = Utils.GetNtlsCon(sp);

        }

        public void SetParameters(string parameters)
        {

        }

        public void Setup()
        {

        }

        public WindowRefreshType DataChange()
        {
            return LSExtensionWindowLib.WindowRefreshType.windowRefreshNone;
        }

        public WindowRefreshType ViewRefresh()
        {
            return LSExtensionWindowLib.WindowRefreshType.windowRefreshNone;
        }

        public void refresh()
        {
        }

        public void SaveSettings(int hKey)
        {
        }

        public void RestoreSettings(int hKey)
        {
        }

        public void Close()
        {

        }

        #endregion


        #region Initilaize

        public void InitializeData()
        {


            //    Debugger.Launch();
            try
            {
                dal = new DataLayer();

                if (DEBUG)
                    dal.MockConnect();
                else
                    dal.Connect(_ntlsCon);




            }
            catch (Exception e)
            {


                MessageBox.Show("Error in  InitializeData " + "/n" + e.Message, mboxHeader);
                Logger.WriteLogFile(e);
            }

        }






        #endregion




        private void textBox1_KeDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {

                LeaveText1();
            }
        }
        private void textBoxSisma_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                textBoxSisma_Leave();
            }

        }
        private void textBoxSisma_Leave()
        {
            try
            {
                string sql1 = "select phrase_description from lims_sys.PHRASE_ENTRY pe inner join lims_sys.PHRASE_HEADER ph  on PH.PHRASE_ID = PE.PHRASE_ID where PH.name = 'Cyto Manager' and pe.PHRASE_NAME = 'Password' and pe.phrase_description = '" + textBoxSisma.Text.Trim() + "'";

                string desc = dal.GetDynamicStr(sql1);
                if (!(desc is null))
                {
                    PrintControl();
                    string sql5 = "select U_CYTO_PRINT_CONTROL_ID from lims_sys.U_CYTO_PRINT_CONTROL where NAME = '" + textBox1.Text.Trim().ToUpper() + "'";


                    string pr_id = dal.GetDynamicStr(sql5);
                    if (!(pr_id is null))
                    {
                        dal.UpdateCytoPrintControl(pr_id);
                        dal.SaveChanges();
                    }

                }
                else
                {

                    labelResult.Text = "סיסמת מנהל שגויה";
                    _10SecStop();
                    textBox1.Focus();


                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("textBoxSisma_Leave" + ex.Message);
            }


        }

        private async Task _10SecStop()
        {
            try
            {
                labelResult.Visible = true;
                textBox1.Enabled = false;

                await Task.Delay(5000);


                textBox1.Enabled = true;
                textBox1.Text = String.Empty;
                textBox1.Focus();
                labelResult.ForeColor = Color.Red;
                labelResult.Visible = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show("_10SecStop" + ex.Message);
            }
        }

        private long _sampleId = 0;
        private long _sdgId;
        private string s = "";
        private void LeaveText1()

        {

            try
            {
                s = textBox1.Text.Trim().ToUpper();
                if (s == String.Empty)
                    return;

                if (!s.StartsWith("C"))
                {

                    labelResult.Text = "לא נמצאה צנצנת עם הברקוד שהוזן";
                    textBoxSisma.Text = "";
                    _10SecStop();
                    return;
                }

                SAMPLE currentSample;
                string u_patholab_num = string.Empty;

                currentSample = dal.FindBy<SAMPLE>(x => x.NAME == s).FirstOrDefault();
                if (currentSample == null)
                {
                    labelResult.Text = "לא נמצאה צנצנת עם הברקוד שהוזן";
                    _10SecStop();
                    return;
                }


                _sdgId = currentSample.SDG.SDG_ID;
                u_patholab_num = currentSample.SDG.SDG_USER.U_PATHOLAB_NUMBER;
                labelMisBdika.Text = "מס' הבדיקה:  " + u_patholab_num;

                _sampleId = currentSample.SAMPLE_ID;

                string STATUS = currentSample.STATUS;
                if (STATUS != "V" && STATUS != "A" && STATUS != "P" && STATUS != "I")
                {

                    labelResult.Text = "סטטוס צנצנת לא תואם";
                    _10SecStop();
                    return;
                }





                int numOfPrintings = 0;
                try
                {
                    numOfPrintings = dal.FindBy<SDG_LOG>(x => x.SDG_ID == _sdgId &&
                                        x.APPLICATION_CODE == "Cyt.Print" &&
                                        (x.DESCRIPTION != null && x.DESCRIPTION.Contains(s))).Count();
                }

                catch
                {

                    //TO DO: bug, check reason.
                    //MessageBox.Show("6");
                }

                if (numOfPrintings == 0 || numOfPrintings == null)
                {
                    PrintControl();

                    currentSample.ALIQUOTs.ToList().ForEach(item => item.ALIQUOT_USER.U_ALIQUOT_STATION = "22");
                    dal.SaveChanges();

                }


                else
                {
                    DateTime lastPrintingTime = DateTime.MinValue;
                    if (numOfPrintings == 1)
                    {

                        lastPrintingTime = dal.FindBy<SDG_LOG>(x => x.SDG_ID == _sdgId && x.APPLICATION_CODE == "Cyt.Print" && x.DESCRIPTION.Contains(s))
                            .FirstOrDefault().TIME;


                        DateTime currentTime = DateTime.Now;
                        TimeSpan timeDifference = currentTime.Subtract(lastPrintingTime);

                        if (timeDifference.TotalMinutes >= 10)
                        {
                            PrintControl();

                            currentSample.ALIQUOTs.ToList().ForEach(item => item.ALIQUOT_USER.U_ALIQUOT_STATION = "23");
                            dal.SaveChanges();

                        }
                        else
                        {
                            labelResult.Visible = true;
                            labelResult.Text = "המדבקה הודפסה ב10 הדקות האחרונות";
                            textBoxSisma.Visible = false;
                            _10SecStop();
                            return;
                        }

                    }
                    else
                    {
                        if (numOfPrintings >= 2)
                        {
                            textBoxSisma.Visible = true;
                            textBox1.Enabled = false;
                            labelResult.Visible = true;
                            label2.Visible = true;
                            labelResult.Text = "נדרשת סיסמת מנהל עבור הדפסה נוספת";
                            textBoxSisma.Focus();
                            labelResult.Visible = false;
                        }

                    }

                }

            }
            catch (Exception e)
            {
                MessageBox.Show("LeaveText1" + e.Message);
                throw;
            }
        }
        private void PrintControl()
        {

            try
            {
                Patholab_XmlService.FireEventXmlHandler fireEvent = new FireEventXmlHandler(sp);
                fireEvent.CreateFireEventXml("SAMPLE", _sampleId, "Print Label");
                bool returnbool = fireEvent.ProcssXml();

                if (returnbool)
                {
                    dal.InsertToSdgLog(_sdgId, "Cyt.Print", (long)_ntlsCon.GetSessionId(), $"הדפסת מדבקה {s} בוצעה בהצלחה");
                    dal.SaveChanges();
                }
                labelResult.Text = "הדפסת המדבקה בוצעה בהצלחה";
                labelResult.ForeColor = Color.Green;
                _10SecStop();
            }
            catch (Exception ex)
            {
                MessageBox.Show("PrintControl" + ex.Message);
            }


        }

        private void labelResult_Click(object sender, EventArgs e)
        {

        }

        private void lblHeader_Click(object sender, EventArgs e)
        {

        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void CytoPrintControlCls_Load(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            _ntlsSite.CloseWindow();
        }
    }
}



