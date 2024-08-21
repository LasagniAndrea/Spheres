﻿using EFS.ACommon;
using EFS.Common.Web;
using EFS.TradeInformation;
using System;
using System.Drawing;
using System.Web.UI;
using System.Web.UI.WebControls;

public partial class Trial_TradeTest : PageBase  , INamingContainer 
{

    private TradeInputGUI _inputTradeGUI;

    protected virtual string NamePlaceHolder
    {
        get { return "phOTCml"; }
    }
    
    protected InputGUI InputGUI
    {
        get { return (InputGUI)_inputTradeGUI; }
    }


    #region private InputTradeGUISessionID
    private string InputTradeGUISessionID
    {
        //get { return GUID + "_GUI"; }
        get { return "GUI"; }
    }
    #endregion

    protected virtual string TitleLeft
    {
        get { return "TitleLeft"; }
    }
    protected virtual string SubTitleLeft
    {
        get { return "SubTitleLeft"; }
    }
    protected virtual string TitleRight
    {
        get { return "TitleRight"; }
    }


    
    protected override void OnInit(EventArgs e)
    {
        base.OnInit(e);

        //if (!IsPostBack)
        //{
        //    _inputTradeGUI = new InputTradeGUI(Request.ServerVariables["APPL_PHYSICAL_PATH"]);
        //}
        //else
        //{
        //    _inputTradeGUI = (InputTradeGUI)Session[InputTradeGUISessionID];
        //}
        ////
        //PageConstruction();
        //// 
        //if (false == IsPostBack)
        //{
        //    AddControl();
        //    SavePlaceHolder();
        //}
        //else
        //{
        //    RestorePlaceHolder(); // Restore ( +  mise à jour du Document Fpml en mode Full)
        //    SavePlaceHolder();
        //}
        
        
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        //

        if (!IsPostBack)
        {
            //Request.ServerVariables["APPL_PHYSICAL_PATH"]
            _inputTradeGUI = new TradeInputGUI(IdMenu.GetIdMenu(IdMenu.Menu.InputTrade), SessionTools.User, @"CCIML\CustomCapture");
            _inputTradeGUI.InitializeFromMenu(SessionTools.CS);
        }
        else
        {
            _inputTradeGUI =  DataCache.GetData<TradeInputGUI>(InputTradeGUISessionID);
        }
        //
        PageConstruction();
        //

        //AddControl();

        if (false == IsPostBack)
        {
            AddControl();
            SavePlaceHolder();
        }
        else
        {
            RestorePlaceHolder(); // Restore ( +  mise à jour du Document Fpml en mode Full)
            SavePlaceHolder();
        }
    }
    protected void Page_Load(object sender, EventArgs e)
    {

    }


    #region protected override PageConstruction
    protected override void PageConstruction()
    {
        HtmlPageTitle titleLeft = new HtmlPageTitle(TitleLeft, SubTitleLeft);
        HtmlPageTitle titleRight = new HtmlPageTitle(TitleRight);
        PageTitle = TitleLeft;
        GenerateHtmlForm();
        FormTools.AddBanniere(this, Form, titleLeft, titleRight, string.Empty, IdMenu.GetIdMenu(IdMenu.Menu.InputTrade));
        PageTools.BuildPage(this, Form, PageFullTitle, string.Empty, false, string.Empty);
    }
    #endregion PageConstruction

    #region protected override GenerateHtmlForm
    protected override void GenerateHtmlForm()
    {
        base.GenerateHtmlForm();
        CreateAndLoadPlaceHolder();
    }
    #endregion

    #region protected virtual CreateAndLoadPlaceHolder
    protected virtual void CreateAndLoadPlaceHolder()
    {
        Table table = new Table
        {
            ID = "tblDetail",
            Width = Unit.Percentage(100),
            Height = Unit.Percentage(100),
            BorderStyle = (IsDebugDesign ? BorderStyle.Solid : BorderStyle.None),
            BorderColor = Color.CornflowerBlue,
            CellPadding = 0,
            CellSpacing = 0
        };

        TableRow tr = new TableRow
        {
            ID = "trDetail"
        };
        // Row 1 - Cell 1
        TableCell td = new TableCell
        {
            ID = "tdDetail"
        };

        PlaceHolder ph = new PlaceHolder
        {
            EnableViewState = false,
            ID = NamePlaceHolder
        };
        //
        td.Controls.Add(ph);
        tr.Cells.Add(td);
        table.Rows.Add(tr);
        //
        CellForm.Controls.Add(table);
    }
    #endregion PlaceHolder (container Trade)

    #region protected virtual SavePlaceholder
    protected virtual void SavePlaceHolder()
    {
        Control ctrl = FindControl(NamePlaceHolder);
        if ((null != ctrl) && (0 < ctrl.Controls.Count))
            InputGUI.Controls = ctrl.Controls;
    }
    #endregion Save Placeholder



    private void AddControl()
    {
        PlaceHolder plh = (PlaceHolder) FindControl(NamePlaceHolder);
        if (null != plh)
        {

            Label lbl = new Label
            {
                //lbl.ID = "lblTest";
                Text = "I'm a Label"
            };

            WCTextBox2 txt = new WCTextBox2(null ,null);


            CheckBox chk = new CheckBox
            {
                //chk.ID = "chkTest";
                AutoPostBack = true
            };
            chk.CheckedChanged += new System.EventHandler(OnCheckedChanged);

            Button but = new Button
            {
                //but.ID = "butTest";
                Text = "OK"
            };
            but.Attributes.Add("onclick", "alert('toto');return true;");  
            

            plh.Controls.Add(txt);
            plh.Controls.Add(new LiteralControl("</br>"));
            plh.Controls.Add(chk);
            plh.Controls.Add(new LiteralControl("&nbsp;"));
            plh.Controls.Add(lbl);
            plh.Controls.Add(new LiteralControl("</br>"));
            plh.Controls.Add(but);
        }


    }


    #region protected override RestorePlaceHolder
    protected void RestorePlaceHolder()
    {
        int i = 0;
        Control ctrl = FindControl(NamePlaceHolder);
        if (null != ctrl)
        {
            if (null != InputGUI)
            {
                ControlCollection controlcollection = InputGUI.Controls;
                if (null != controlcollection)
                {
                    try
                    {
                        while (0 != controlcollection.Count)
                        {
                            ctrl.Controls.Add(controlcollection[0]);
                            i += 1;
                        }
                    }
                    catch (Exception ex)
                    {
                        ctrl.Controls.AddAt(0, new LiteralControl(System.Environment.NewLine + ex.Message.ToString()));
                    }
                }
            }
        }
    }
    #endregion RestorePlaceHolder

    #region protected override OnPreRender
    protected override void OnPreRender(EventArgs e)
    {
        DataCache.SetData<TradeInputGUI>(InputTradeGUISessionID, _inputTradeGUI);
        base.OnPreRender(e);
    }
    #endregion OnPreRender


    private void OnCheckedChanged(object sender, EventArgs e)
    {
        Label lbl = null;
        try
        {
            lbl = (Label)FindControl("lblTest");
        }
        catch
        {
        }
        //
        if (null != lbl)
        {
            try
            {
                lbl = (Label)Controls[0];
            }
            catch { }
        }
        //
        if (null != lbl)
        {
            lbl.Text = "OnCheckedChanged";
        }
    }

}
