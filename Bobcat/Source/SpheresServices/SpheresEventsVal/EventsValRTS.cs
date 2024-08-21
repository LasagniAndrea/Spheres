#region Using Directives
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
//
using EFS.ACommon;
using EFS.Actor;
using EFS.ApplicationBlocks.Data;
using EFS.Common;
using EFS.Common.Log;
using EFS.Common.MQueue;
using EFS.LoggerClient;
using EFS.LoggerClient.LoggerService;
using EFS.SpheresService;
using EFS.TradeInformation;
using EFS.Tuning;
//
using EfsML;
using EfsML.Business;
using EfsML.Curve;
using EfsML.Enum;
using EfsML.Enum.Tools;
using EfsML.Interface;
using EfsML.v30.PosRequest;
//
using FixML.Interface;
//
using FpML.Enum;
using FpML.Interface;
#endregion Using Directives

namespace EFS.Process
{
    #region RTSQuoteInfo
    public class RTSQuoteInfo : QuoteInfo, ICloneable
    {
        #region Members

        #endregion Members
        #region Constructors
        public RTSQuoteInfo() { }
        public RTSQuoteInfo(EventsValProcessRTS pProcess, NextPreviousEnum pType) :
            this(pProcess, pType, pProcess.ReturnSwapContainer.ClearingBusinessDate.Date)
        {
        }
        public RTSQuoteInfo(EventsValProcessRTS pProcess, NextPreviousEnum pType, Nullable<DateTime> pDate)
            : base(pProcess, pType, pDate)
        {
            PosKeepingAsset posKeepingAsset = pProcess.ReturnSwapContainer.ProductBase.CreatePosKeepingAsset(pProcess.ReturnSwapContainer.AssetReturnLeg.First);
            posKeepingAsset.idAsset = pProcess.ReturnSwapContainer.AssetReturnLeg.Second;
            posKeepingAsset.identifier = pProcess.ReturnSwapContainer.Identifier;
            dtQuote = posKeepingAsset.GetOfficialCloseQuoteTime(dtBusiness);
        }
        #endregion Constructors
        #region Methods
        #region Clone
        public object Clone()
        {
            RTSQuoteInfo clone = new RTSQuoteInfo
            {
                dtBusiness = this.dtBusiness,
                dtQuote = this.dtQuote,
                processCacheContainer = this.processCacheContainer,
                rowState = this.rowState
            };
            return clone;
        }
        #endregion Clone
        #endregion Methods
    }
    #endregion RTSQuoteInfo
    #region EventsValProcessRTS
    // EG 20231127 [WI749] Implementation Return Swap : Partial class
    public partial class EventsValProcessRTS : EventsValProcessBase
    {
        #region Members
        private readonly CommonValParameters m_Parameters;
        private ReturnSwapContainer m_ReturnSwapContainer;
        #endregion Members
        #region Accessors

        #region IsQuoteReturnLeg
        private bool IsQuoteReturnLeg
        {
            get { return (null != m_Quote) && (m_Quote.idAsset == m_ReturnSwapContainer.IdAssetReturnLeg); }
        }
        #endregion IsQuoteReturnLeg
        #region FundingRateQuotationSide
        // EG 20150311 POC - BERKELEY New
        public override QuotationSideEnum FundingRateQuotationSide
        {
            get
            {
                QuotationSideEnum quotationSide = QuotationSideEnum.OfficialClose;
                if (m_ReturnSwapContainer.AssetReturnLeg.First == Cst.UnderlyingAsset.FxRateAsset)
                {
                    bool isDealerBuyer = m_ReturnSwapContainer.IsDealerBuyerOrSeller(BuyerSellerEnum.BUYER);
                    quotationSide = (isDealerBuyer ? QuotationSideEnum.Ask : QuotationSideEnum.Bid);
                }
                return quotationSide;
            }
        }
        #endregion FundingRateQuotationSide

        #region Multiplier
        // EG 20150306 [POC-BERKELEY] : New
        protected override decimal Multiplier
        {
            get
            {
                decimal multiplier = 1;
                if (m_ReturnSwapContainer.ReturnLeg.Underlyer.UnderlyerSingleSpecified &&
                    m_ReturnSwapContainer.ReturnLeg.Underlyer.UnderlyerSingle.NotionalBaseSpecified)
                {
                    multiplier = m_ReturnSwapContainer.ReturnLeg.Underlyer.UnderlyerSingle.NotionalBase.Amount.DecValue / m_ReturnSwapContainer.MainOpenUnits.Value;
                }
                return multiplier;
            }
        }
        #endregion Multiplier
        #region Parameters
        public override CommonValParameters Parameters
        {
            get { return m_Parameters; }
        }
        #endregion Parameters

        #region ReturnSwapContainer
        public ReturnSwapContainer ReturnSwapContainer
        {
            get { return m_ReturnSwapContainer; }
        }
        #endregion ReturnSwapContainer
        #endregion Accessors
        #region Constructors
        public EventsValProcessRTS(EventsValProcess pProcess, DataSetTrade pDsTrade, EFS_TradeLibrary pTradeLibrary, IProduct pProduct)
            : base(pProcess, pDsTrade, pTradeLibrary, pProduct)
        {
            m_Parameters = new CommonValParametersRTS();
            if (ProcessBase.ProcessCallEnum.Master == pProcess.ProcessCall)
                pProcess.ProcessCacheContainer = new ProcessCacheContainer(pProcess.Cs, (IProductBase)pProduct);

        }
        #endregion Constructors
        #region Methods

        #region EndOfInitialize
        // EG 20180502 Analyse du code Correction [CA2214]
        public override void EndOfInitialize()
        {
            if (false == m_tradeLibrary.DataDocument.CurrentProduct.IsStrategy)
            {
                Initialize();
                InitializeDataSetEvent();
            }
        }
        #endregion EndOfInitialize
        #region InitializeDataSetEvent
        /// <summary>
        /// Cette méthode override la méthode virtuelle pour le traitement EOD
        /// Dans ce cas 
        /// 1./ Le nombre de tables EVENTXXX chargées est réduit : EVENT|EVENTCLASS|EVENTDET|EVENTASSET
        /// 2./ La date DTBUSINESS est utilisé pour restreindre le nombre d'EVTS chargé tels que DtBusiness between DTSTARTADJ and DTENDADJ
        /// </summary>
        // EG 20150612 [20665] Refactoring : Chargement DataSetEventTrade
        // EG 20150617 [20665] 
        public override void InitializeDataSetEvent()
        {
            m_DsEvents = new DataSetEventTrade(m_CS, SlaveDbTransaction,  ParamIdT);
            Nullable<DateTime> dtBusiness = null;
            Nullable<DateTime> dtBusinessPrev = null; // Utilisé pour calcul TMG
            if (IsEndOfDayProcess)
            {
                dtBusiness = m_Quote.time;
                RTSQuoteInfo _quotePrev = new RTSQuoteInfo(this, NextPreviousEnum.Previous);
                dtBusinessPrev = _quotePrev.dtBusiness;
            }
            m_DsEvents.Load(EventTableEnum.Class | EventTableEnum.Asset | EventTableEnum.Detail, dtBusiness, dtBusinessPrev);
        }
        #endregion InitializeDataSetEvent

        #region CalculationReturnLeg
        /// EG 20141224 [20566]
        /// EG 20170510 [23153] Refactoring
        // EG 20190327 [MIGRATION VCL] Correction SlaveDbTransaction sur Query Qty
        private Cst.ErrLevel CalculationReturnLeg(RTSQuoteInfo pQuote)
        {
            RTSQuoteInfo _quotePrev = new RTSQuoteInfo(this, NextPreviousEnum.Previous);
            // EG 20151102 [20979] Refactoring
            // EG 20190327 [MIGRATION VCL] Correction SlaveDbTransaction sur Query Qty
            m_PosAvailableQuantity = PosKeepingTools.GetAvailableQuantity(m_EventsValProcess.Cs, SlaveDbTransaction, pQuote.dtBusiness, m_EventsValProcess.CurrentId);
            // EG 20170510 [23153]
            if ((null != m_Quote) && m_Quote.eodComplementSpecified)
                m_PosQuantityPrevAndActionsDay = m_Quote.eodComplement.posQuantityPrevAndActionsDay;
            else
                m_PosQuantityPrevAndActionsDay = PosKeepingTools.GetPreviousAvailableQuantity(m_EventsValProcess.Cs, SlaveDbTransaction, pQuote.dtBusiness.Date, m_EventsValProcess.CurrentId);


            Calculation_LPC_AMT(pQuote, _quotePrev);

            #region Calculations at ClearingDate+n <= DtMarket (HORS EOD)
            bool isQuotationToCalc = (false == IsEndOfDayProcess);
            int guard = 0;

            DateTime dtQuotation = _quotePrev.dtQuote;
            RTSQuoteInfo _savQuoteInfo = (RTSQuoteInfo)_quotePrev.Clone();
            while (isQuotationToCalc)
            {
                guard++;
                if (guard == 999)
                {
                    string msgException = "Incoherence during the calculation. Infinite loop detected" + Cst.CrLf;
                    throw new Exception(msgException);
                }

                RTSQuoteInfo _quoteNextInfo = new RTSQuoteInfo(this, NextPreviousEnum.Next, dtQuotation);
                isQuotationToCalc = (_quoteNextInfo.dtBusiness <= m_EntityMarketInfo.DtMarket);
                if (isQuotationToCalc)
                    isQuotationToCalc = _quoteNextInfo.SetQuote(this);

                if (isQuotationToCalc)
                {
                    m_PosAvailableQuantityPrev = m_PosAvailableQuantity;
                    // EG 20151102 [20979] Refactoring
                    // EG 20190327 [MIGRATION VCL] Correction SlaveDbTransaction sur Query Qty
                    m_PosAvailableQuantity = PosKeepingTools.GetAvailableQuantity(m_EventsValProcess.Cs, SlaveDbTransaction, _quoteNextInfo.dtBusiness, m_EventsValProcess.CurrentId);
                    Calculation_LPC_AMT(_quoteNextInfo, _savQuoteInfo);
                }
                if ((DataRowState.Modified == _quoteNextInfo.rowState) || (false == isQuotationToCalc))
                    break;

                _savQuoteInfo = (RTSQuoteInfo)_quoteNextInfo.Clone();
            }
            #endregion Calculations at ClearingDate+n <= DtMarket (HORS EOD)
            return Cst.ErrLevel.SUCCESS;
        }
        #endregion CalculationReturnLeg

        #region CalculationInterestLegInfo
        public class CalculationInterestLegInfo
        {
            #region Members
            protected int m_IdE;
            protected EventsValProcessBase m_EventsValProcess;

            protected DateTime m_CommonValDate;
            public DateTime startDate;
            public DateTime endDate;
            public DateTime startDateUnAdj;
            protected DateTime endDateUnAdj;
            protected DateTime m_RateCutOffDate;
            protected string m_Currency;
            protected int m_IdAsset;

            protected bool m_FinalRateRoundingSpecified;
            protected IRounding m_FinalRateRounding;
            // EG 20150219 Change decimal to Nullable<decimal>
            public Nullable<decimal> notional;
            public Nullable<decimal> multiplier;
            // EG 20150309 POC - BERKELEY Spread = (Somme des spreads)
            public Nullable<decimal> spread;
            // EG 20150309 POC - BERKELEY New PIP
            public Nullable<Decimal> percentageInPoint;

            public string dayCountFraction;
            public IInterval intervalFrequency;

            // EG 20150706 [21021] Nullable<int>
            public Pair<Nullable<int>, Nullable<int>> payer;
            // EG 20150706 [21021] Nullable<int>
            public Pair<Nullable<int>, Nullable<int>> receiver;

            #endregion
            #region Accessors
            #region EventsValProcess
            public EventsValProcessBase EventsValProcess
            {
                get { return m_EventsValProcess; }
            }
            #endregion
            #region public Currency
            public string Currency
            {
                get { return m_Currency; }
            }
            #endregion
            #region public IdE
            public int IdE
            {
                get { return m_IdE; }
            }
            #endregion
            #endregion Accessors
            #region Constructors
            public CalculationInterestLegInfo(DateTime pDtBusiness, EventsValProcessBase pEventsValProcess, DataRow pRowFDA,
                Pair<IReturnLeg, IReturnLegMainUnderlyer> pReturnLeg, Pair<IInterestLeg, IInterestCalculation> pCurrentInterestLeg)
            {
                SetInfoBase(pDtBusiness, pEventsValProcess, pRowFDA, pReturnLeg, pCurrentInterestLeg);
                SetParameter();
            }
            #endregion Constructors
            #region Methods
            #region SetInfoBase
            /// <summary>
            /// Initialisation des données pour calcul du FDA
            ///  1. Sans Reset du notionnel (BFL)
            ///     FDA (en QCU) = "BCU Amount(1)" * "Taux lu" * "Nbre de jours(2) " " * "DCF"
            ///  2. Avec Reset du notionnel
            ///     FDA (en BCU) = "QCU Amount(3)" * "Taux lu" * "Nbre de jours(2) " " * "DCF"
            /// 
            /// </summary>
            /// <param name="pDtBusiness"></param>
            /// <param name="pEventsValProcess"></param>
            /// <param name="pRowFDA"></param>
            /// <param name="pReturnLeg"></param>
            /// <param name="pCurrentInterestLeg"></param>
            // FI 20141215 [20570] Modify
            // EG 20150311 [POC - BERKELEY] Notionel de base pour calcul du FDA (FOREX)
            // EG 20180205 [23769] Upd DataDocumentContainer parameter (substitution to the static class EFS_CURRENT)  
            private void SetInfoBase(DateTime pDtBusiness, EventsValProcessBase pEventsValProcess, DataRow pRowFDA,
                Pair<IReturnLeg, IReturnLegMainUnderlyer> pReturnLeg, Pair<IInterestLeg, IInterestCalculation> pCurrentInterestLeg)
            {
                m_EventsValProcess = pEventsValProcess;
                m_CommonValDate = pDtBusiness;//PL 20150126 Affectation de m_CommonValDate pour en disposer plus tard dans la méthode UpdatingRow().
                m_IdE = Convert.ToInt32(pRowFDA["IDE"]);
                startDate = Convert.ToDateTime(pRowFDA["DTSTARTADJ"]);
                startDateUnAdj = Convert.ToDateTime(pRowFDA["DTSTARTUNADJ"]);
                endDate = Convert.ToDateTime(pRowFDA["DTENDADJ"]);
                endDateUnAdj = Convert.ToDateTime(pRowFDA["DTENDUNADJ"]);

                string payerhRef = pCurrentInterestLeg.First.PayerPartyReference.HRef;
                string receiverhRef = pCurrentInterestLeg.First.ReceiverPartyReference.HRef;
                DataDocumentContainer dataDocument = m_EventsValProcess.TradeLibrary.DataDocument;
                // EG 20150706 [21021]
                payer = new Pair<Nullable<int>, Nullable<int>>(dataDocument.GetOTCmlId_Party(payerhRef), dataDocument.GetOTCmlId_Book(payerhRef));
                receiver = new Pair<Nullable<int>, Nullable<int>>(dataDocument.GetOTCmlId_Party(receiverhRef), dataDocument.GetOTCmlId_Book(receiverhRef));

                // FI 20141215 [20570] WARNING: Si évènement FDA d'une journée, généré dans le cadre d'un processus EOD, alors le calcul d'intérêt "inclut" dtFin. 
                // On ajoute ici une journée pour que le calcul des intérêts porte bien sur DTENDADJ inclus
                if (pEventsValProcess.IsEndOfDayProcess && pReturnLeg.First.IsDailyPeriod && pCurrentInterestLeg.First.IsPeriodRelativeToReturnLeg)
                {
                    endDate = endDate.AddDays(1);
                    endDateUnAdj = endDate;
                }

                DataRow[] rowAssets = m_EventsValProcess.GetRowAsset(Convert.ToInt32(pRowFDA["IDE"]));
                if ((null != rowAssets) && (0 < rowAssets.Length))
                {
                    DataRow rowAsset = rowAssets[0];
                    #region FloatingRate
                    if ((null != rowAsset) && (false == Convert.IsDBNull(rowAsset["IDASSET"])))
                    {
                        m_IdAsset = Convert.ToInt32(rowAsset["IDASSET"]);

                        // EG 20150309 POC - BERKELEY Lecture du PIP
                        if (pCurrentInterestLeg.Second.SqlAssetSpecified &&
                            (pCurrentInterestLeg.Second.SqlAsset.AssetCategory == Cst.UnderlyingAsset.RateIndex))
                        {
                            SQL_AssetRateIndex sql_AssetRateIndex = pCurrentInterestLeg.Second.SqlAsset as SQL_AssetRateIndex;
                            if (sql_AssetRateIndex.Idx_IndexUnit == Cst.IdxUnit_currency.ToString())
                            {
                                if (pReturnLeg.Second.SqlAssetSpecified &&
                                    (pReturnLeg.Second.UnderlyerAsset == Cst.UnderlyingAsset.FxRateAsset))
                                {
                                    SQL_AssetFxRate sql_AssetFxRate = pReturnLeg.Second.SqlAsset as SQL_AssetFxRate;
                                    percentageInPoint = sql_AssetFxRate.PercentageInPoint;
                                }
                            }
                        }
                    }
                    #endregion FloatingRate
                }

                if (null == pCurrentInterestLeg.First.Efs_InterestLeg)
                {
                    pCurrentInterestLeg.First.Efs_InterestLeg = new EFS_InterestLeg(m_EventsValProcess.Process.Cs, dataDocument);
                    pCurrentInterestLeg.First.Efs_InterestLeg.InitMembers(pCurrentInterestLeg.First, pCurrentInterestLeg.First.Notional);
                }

                // Si Notional est celui de la jambe ReturnLeg.
                notional = pCurrentInterestLeg.First.Efs_InterestLeg.notional.Amount.DecValue;
                m_Currency = pCurrentInterestLeg.First.Efs_InterestLeg.notional.Currency;

                if (pReturnLeg.Second.UnderlyerAsset == Cst.UnderlyingAsset.FxRateAsset)
                {
                    // Si NotionalReset alors devient la MarketValue du jour
                    if ((pCurrentInterestLeg.First.Efs_InterestLeg.notionalHRef == pReturnLeg.First.Notional.Id) &&
                        pReturnLeg.First.RateOfReturn.NotionalResetSpecified && pReturnLeg.First.RateOfReturn.NotionalReset.BoolValue)
                    {
                        // FDA Currency = CURRENCY (BCU).
                        m_Currency = pReturnLeg.Second.NotionalBase.Currency;
                    }
                    else if (pReturnLeg.Second.NotionalBaseSpecified)
                    {
                        // Notional = AMOUNT (BCU).
                        notional = pReturnLeg.Second.NotionalBase.Amount.DecValue;
                    }
                }
                else
                {
                    // Si NotionalReset alors devient la MarketValue du jour
                    if ((pCurrentInterestLeg.First.Efs_InterestLeg.notionalHRef == pReturnLeg.First.Notional.Id) &&
                        pReturnLeg.First.RateOfReturn.NotionalResetSpecified && pReturnLeg.First.RateOfReturn.NotionalReset.BoolValue)
                        notional = m_EventsValProcess.GetAmount(pDtBusiness, EventTypeFunc.MarketValue);
                }


                DataRow rowDetail = m_EventsValProcess.GetRowDetail(m_IdE);
                dayCountFraction = rowDetail["DCF"].ToString();
                multiplier = Convert.IsDBNull(rowDetail["MULTIPLIER"]) ? 1 : Convert.ToDecimal(rowDetail["MULTIPLIER"]);
                if (false == Convert.IsDBNull(rowDetail["SPREAD"]))
                    spread = Convert.ToDecimal(rowDetail["SPREAD"]);
                if (false == Convert.IsDBNull(rowDetail["PIP"]))
                    percentageInPoint = Convert.ToDecimal(rowDetail["PIP"]);
            }
            #endregion SetInfoBase
            #region SetParameter
            public void SetParameter()
            {
                CommonValParameterRTS parameter = (CommonValParameterRTS)m_EventsValProcess.Parameters[m_EventsValProcess.ParamInstrumentNo, m_EventsValProcess.ParamStreamNo];
                #region FloatingRate
                if ((0 != m_IdAsset) && (null == parameter.Rate))
                {
                    parameter.Rate = new SQL_AssetRateIndex(parameter.CS, SQL_AssetRateIndex.IDType.IDASSET, m_IdAsset)
                    {
                        WithInfoSelfCompounding = Cst.IndexSelfCompounding.CASHFLOW
                    };
                }
                #endregion FloatingRate
                #region Calculation Period Frequency
                intervalFrequency = parameter.CalculationPeriodFrequency;
                #endregion Calculation Period Frequency
                #region FinalRateRounding
                m_FinalRateRounding = parameter.FinalRateRounding;
                #endregion FinalRateRounding
            }
            #endregion SetParameter
            #endregion Methods
        }
        #endregion CalculationInterestLegInfo
        #region CalculationInterestLegResetInfo
        public class CalculationInterestLegResetInfo
        {
            #region Members
            protected EventsValProcessBase m_EventsValProcess;
            protected int m_IdE;
            protected DateTime m_CommonValDate;
            protected DateTime m_StartDate;
            protected DateTime m_EndDate;
            public DateTime resetDate;
            protected DateTime m_FixingDate;
            protected DateTime m_RateCutOffDate;
            protected DateTime m_ObservedRateDate;
            protected DateTime m_EndPeriodDate;
            protected int m_IdAsset;
            protected bool m_RateTreatmentSpecified;
            protected RateTreatmentEnum m_RateTreatment;
            protected IInterval m_PaymentFrequency;
            protected int m_RoundingPrecision;

            protected IRounding m_RateRounding;

            protected DataRow[] m_RowAssets;
            #endregion Members
            #region Accessors
            #region IdE
            public int IdE
            {
                get { return m_IdE; }
            }
            #endregion
            #endregion Accessors
            #region Constructors
            public CalculationInterestLegResetInfo(DateTime pDtBusiness, EventsValProcessBase pEventsValProcess, DataRow pRowReset)
            {
                SetInfoBase(pDtBusiness, pEventsValProcess, pRowReset);
                SetParameter();
            }
            public CalculationInterestLegResetInfo(DateTime pDtBusiness, EventsValProcessBase pEventsValProcess, DataRow pRowReset, int pIdAsset)
            {
                SetInfoBase(pDtBusiness, pEventsValProcess, pRowReset);
                m_IdAsset = pIdAsset;
                m_ObservedRateDate = m_FixingDate;
                DataRow rowCalcPeriod = pRowReset.GetParentRow(m_EventsValProcess.DsEvents.ChildEvent);
                m_EndPeriodDate = Convert.ToDateTime(rowCalcPeriod["DTENDADJ"]);
                SetParameter();
            }
            #endregion Constructors
            #region Methods
            #region SetInfoBase
            private void SetInfoBase(DateTime pDtBusiness, EventsValProcessBase pEventsValProcess, DataRow pRowReset)
            {
                m_EventsValProcess = pEventsValProcess;
                m_CommonValDate = pDtBusiness;
                m_IdE = Convert.ToInt32(pRowReset["IDE"]);
                m_StartDate = Convert.ToDateTime(pRowReset["DTSTARTADJ"]);
                m_EndDate = Convert.ToDateTime(pRowReset["DTENDADJ"]);
                m_RowAssets = m_EventsValProcess.GetRowAsset(Convert.ToInt32(pRowReset["IDE"]));
                DataRow[] rowEventClass = pRowReset.GetChildRows(m_EventsValProcess.DsEvents.ChildEventClass);
                foreach (DataRow dr in rowEventClass)
                {
                    string eventClass = dr["EVENTCLASS"].ToString();
                    if (EventClassFunc.IsGroupLevel(eventClass))
                        resetDate = Convert.ToDateTime(dr["DTEVENT"]);
                    else if (EventClassFunc.IsFixing(eventClass))
                        m_FixingDate = Convert.ToDateTime(dr["DTEVENT"]);
                }
            }
            #endregion SetInfoBase
            #region SetParameter
            public void SetParameter()
            {
                Cst.ErrLevel ret;
                CommonValParameterRTS parameter = (CommonValParameterRTS)m_EventsValProcess.Parameters[m_EventsValProcess.ParamInstrumentNo, m_EventsValProcess.ParamStreamNo];
                m_PaymentFrequency = parameter.CalculationPeriodFrequency;

                if (0 != m_IdAsset)
                {
                    int precisionRate = Convert.ToInt32(parameter.Rate.Idx_RoundPrec);
                    m_RoundingPrecision = Math.Max(precisionRate, 3);
                }
                else
                {
                    RoundingDirectionEnum direction = (RoundingDirectionEnum)StringToEnum.Parse(parameter.Rate.Idx_RoundDir, RoundingDirectionEnum.Nearest);
                    m_RateRounding = m_PaymentFrequency.GetRounding(direction, parameter.Rate.Idx_RoundPrec);
                }

                #region RateTreatment
                ret = parameter.RateTreatment(out m_RateTreatment);
                m_RateTreatmentSpecified = (Cst.ErrLevel.SUCCESS == ret);
                #endregion RateTreatment
            }
            #endregion SetParameter
            #endregion
        }
        #endregion ResetInfo

        #region CalculationInterestLegEvent
        public class CalculationInterestLegEvent : CalculationInterestLegInfo
        {
            #region Members
            protected CalculationInterestLegResetEvent[] m_ResetEvents;
            public Decimal averagedRate;
            public Decimal capFlooredRate;
            public Nullable<Decimal> calculatedRate;
            public Nullable<Decimal> calculatedAmount;
            public Nullable<Decimal> roundedCalculatedAmount;
            public Nullable<Decimal> compoundCalculatedAmount;
            #endregion Members
            #region Accessors
            public CalculationInterestLegResetEvent[] ResetEvents
            {
                get { return m_ResetEvents; }
            }
            #endregion Accessors

            #region Constructors
            /// <summary>
            /// Calcul des FDA (Periods / Reset)
            /// </summary>
            /// <param name="pDtBusiness"></param>
            /// <param name="pEventsValProcess"></param>
            /// <param name="pRowINL"></param>
            /// <param name="pRowFDA"></param>
            /// <param name="pReturnLeg"></param>
            /// <param name="pInterestLeg"></param>
            //PL 20150126 Add pPosAvailableQuantity
            // EG 20180502 Analyse du code Correction [CA2200]
            public CalculationInterestLegEvent(DateTime pDtBusiness, EventsValProcessBase pEventsValProcess, DataRow pRowINL, DataRow pRowFDA,
                Pair<IReturnLeg, IReturnLegMainUnderlyer> pReturnLeg, Pair<IInterestLeg, IInterestCalculation> pCurrentInterestLeg, decimal pPosAvailableQuantity)
                : base(pDtBusiness, pEventsValProcess, pRowFDA, pReturnLeg, pCurrentInterestLeg)
            {
                //bool isRowUpdating = true;
                try
                {
                    EFS_CalculAmount calculAmount;
                    DataRow[] rowResets = pRowFDA.GetChildRows(m_EventsValProcess.DsEvents.ChildEvent);
                    if ((0 != rowResets.Length) && (EventTypeFunc.IsFloatingRate(pRowINL["EVENTTYPE"].ToString())))
                    {
                        #region FloatingRate
                        CalculationInterestLegResetEvent resetEvent;
                        ArrayList aResetEvent = new ArrayList();
                        #region Reset Process
                        foreach (DataRow rowReset in rowResets)
                        {
                            if (m_EventsValProcess.IsRowMustBeCalculate(rowReset))
                            {
                                // EG 20150219 Test pPosAvailableQuantity
                                if (0 < pPosAvailableQuantity)
                                {

                                    #region CapFloorPeriods Excluded
                                    if (EventTypeFunc.IsCapFloorLeg(rowReset["EVENTTYPE"].ToString()))
                                    {
                                        rowReset["IDSTCALCUL"] = StatusCalculFunc.CalculatedAndRevisable;
                                        continue;
                                    }
                                    #endregion CapFloorPeriods Excluded

                                    m_EventsValProcess.SetRowAssetToFundingAmountOrReset(rowReset, pCurrentInterestLeg.Second);

                                    CommonValFunc.SetRowCalculated(rowReset);
                                    resetEvent = new CalculationInterestLegResetEvent(m_CommonValDate, m_EventsValProcess, rowReset, m_IdAsset);
                                    aResetEvent.Add(resetEvent);
                                    if (false == CommonValFunc.IsRowEventCalculated(rowReset))
                                        break;
                                }
                                else
                                {
                                    rowReset.Delete();
                                }
                            }
                        }
                        m_ResetEvents = (CalculationInterestLegResetEvent[])aResetEvent.ToArray(typeof(CalculationInterestLegResetEvent));
                        #endregion Reset Process
                        //
                        // EG 20150219 Test pPosAvailableQuantity
                        if (0 < pPosAvailableQuantity)
                        {

                            #region Final CalculationPeriod Process
                            if (m_EventsValProcess.IsRowsEventCalculated(rowResets))
                            {
                                #region FinalRateRounding / AmountCalculation / Compounding
                                calculatedRate = ((CalculationInterestLegResetEvent)m_ResetEvents.GetValue(0)).observedRate;
                                FinalRateRounding();
                                //
                                #region AmountCalculation
                                calculAmount = new EFS_CalculAmount(notional, multiplier, calculatedRate, spread, startDate, endDate, dayCountFraction,
                                    intervalFrequency, percentageInPoint);
                                calculatedAmount = calculAmount.calculatedAmount;
                                #endregion AmountCalculation
                                //
                                DataRow rowDetail = m_EventsValProcess.GetRowDetail(Convert.ToInt32(pRowFDA["IDE"]));
                                if (null != rowDetail)
                                {
                                    rowDetail["RATE"] = calculatedRate.HasValue ? calculatedRate : Convert.DBNull;
                                    rowDetail["MULTIPLIER"] = (multiplier.HasValue && (1 != multiplier)) ? multiplier.Value : Convert.DBNull;
                                    rowDetail["SPREAD"] = (spread.HasValue && (0 != spread)) ? spread.Value : Convert.DBNull;
                                    rowDetail["NOTIONALAMOUNT"] = notional ?? Convert.DBNull;
                                    rowDetail["PIP"] = percentageInPoint ?? Convert.DBNull;
                                }
                                #endregion FinalRateRounding / AmountCalculation / Compounding
                            }
                            #endregion Final CalculationPeriod Process
                        }
                        #endregion FloatingRate
                    }
                    else if (EventTypeFunc.IsFixedRate(pRowINL["EVENTTYPE"].ToString()))
                    {
                        #region FixedRate
                        if (0 < pPosAvailableQuantity)
                        {

                            DataRow rowDetail = m_EventsValProcess.GetRowDetail(Convert.ToInt32(pRowFDA["IDE"]));
                            if (null != rowDetail)
                                calculatedRate = Convert.ToDecimal(rowDetail["RATE"]);
                            #region Amount Calculation
                            calculAmount = new EFS_CalculAmount(notional, calculatedRate, startDate, endDate, dayCountFraction, intervalFrequency);
                            calculatedAmount = calculAmount.calculatedAmount;
                            rowDetail["NOTIONALAMOUNT"] = notional ?? Convert.DBNull;
                            #endregion Amount Calculation
                        }
                        #endregion FixedRate
                    }
                }
                catch (Exception)
                {
                    CommonValProcessBase.ResetRowCalculated(pRowFDA);
                    m_EventsValProcess.SetRowStatus(pRowFDA, Tuning.TuningOutputTypeEnum.OEE);
                    throw;
                }
                finally
                {
                    if (0 < pPosAvailableQuantity)
                        UpdatingRow(pRowFDA, pPosAvailableQuantity);
                }
            }
            #endregion Constructors
            //
            #region Methods
            #region FinalRateRounding
            private void FinalRateRounding()
            {
                if ((null != m_FinalRateRounding) && calculatedRate.HasValue)
                {
                    EFS_Round round = new EFS_Round(m_FinalRateRounding, calculatedRate.Value);
                    calculatedRate = round.AmountRounded;
                }
            }
            #endregion FinalRateRounding
            #region UpdatingRow
            private void UpdatingRow(DataRow pRow, decimal pPosAvailableQuantity)
            {
                // EG 20150120 Arrondi du FDA 
                if (calculatedAmount.HasValue)
                    roundedCalculatedAmount = m_EventsValProcess.RoundingCurrencyAmount(m_Currency, calculatedAmount.Value);
                //compoundCalculatedAmount = calculatedAmount;
                pRow["UNIT"] = m_Currency;
                pRow["UNITTYPE"] = UnitTypeEnum.Currency.ToString();
                // PL 20150121 Arrondi du FDA (suite)
                // PL 20150129 A nouveau NON Arrondi du FDA 
                pRow["VALORISATION"] = (calculatedAmount.HasValue ? Math.Abs(calculatedAmount.Value) : Convert.DBNull);
                //pRow["VALORISATION"] = roundedCalculatedAmount;
                pRow["UNITSYS"] = m_Currency;
                pRow["UNITTYPESYS"] = UnitTypeEnum.Currency.ToString();
                pRow["VALORISATIONSYS"] = (calculatedAmount.HasValue ? Math.Abs(calculatedAmount.Value) : Convert.DBNull);
                pRow["IDA_PAY"] = Convert.DBNull;
                pRow["IDB_PAY"] = Convert.DBNull;
                pRow["IDA_REC"] = Convert.DBNull;
                pRow["IDB_REC"] = Convert.DBNull;
                //pRow["VALORISATIONSYS"] = roundedCalculatedAmount; 
                if (calculatedAmount.HasValue)
                {
                    if (0 < calculatedAmount)
                        CommonValFunc.SetPayerReceiver(pRow, payer.First, payer.Second, receiver.First, receiver.Second);
                    else
                        CommonValFunc.SetPayerReceiver(pRow, receiver.First, receiver.Second, payer.First, payer.Second);
                }
                CommonValFunc.SetRowCalculated(pRow);
                m_EventsValProcess.SetRowStatus(pRow, Tuning.TuningOutputTypeEnum.OES);

                DataRow rowDetail = m_EventsValProcess.GetRowDetail(m_IdE);
                if (null != rowDetail)
                {
                    if (DtFunc.IsDateTimeFilled(m_CommonValDate))
                    {
                        EFS_DayCountFraction dcf = new EFS_DayCountFraction(startDate, endDate, dayCountFraction, intervalFrequency);
                        rowDetail["DCFNUM"] = dcf.Numerator;
                        //PL 20150128 Valorisation de DCFDEN (Potentiellement utile au flux XML de messagerie)
                        rowDetail["DCFDEN"] = dcf.Denominator;
                        //PL 20150126 Valorisation de TOTALOFYEAR et TOTALOFDAY (Utile au flux XML de messagerie)
                        rowDetail["TOTALOFYEAR"] = dcf.NumberOfCalendarYears;
                        rowDetail["TOTALOFDAY"] = dcf.TotalNumberOfCalendarDays;
                    }
                    //PL 20150126 Valorisation de INTEREST
                    rowDetail["INTEREST"] = (calculatedAmount.HasValue ? Math.Abs(calculatedAmount.Value) : Convert.DBNull);
                    //PL 20150126 Valorisation de DAILYQUANTITY
                    rowDetail["DAILYQUANTITY"] = pPosAvailableQuantity;

                    rowDetail["RATE"] = (calculatedRate.HasValue) ? calculatedRate : Convert.DBNull;
                    rowDetail["MULTIPLIER"] = (multiplier.HasValue && (1 != multiplier)) ? multiplier.Value : Convert.DBNull;
                    rowDetail["SPREAD"] = (spread.HasValue && (0 != spread)) ? spread.Value : Convert.DBNull;
                    rowDetail["NOTIONALAMOUNT"] = notional ?? Convert.DBNull;
                    rowDetail["PIP"] = percentageInPoint ?? Convert.DBNull;
                }

            }
            #endregion UpdatingRow
            #endregion Methods
        }
        #endregion CalculationInterestLegEvent

        #region CalculationInterestLegResetEvent
        public class CalculationInterestLegResetEvent : CalculationInterestLegResetInfo
        {
            #region Members
            protected EFS_SelfAveragingEvent[] m_SelfAveragingEvents;
            //
            /// <summary>
            /// <para>Taux lu ou estimé s'il n'existe pas des selfAverage</para>
            /// <para>Taux Compound s'il existe des selfAverage</para>
            /// </summary>
            public Nullable<decimal> observedRate;
            /// <summary>
            /// Taux obtenu après traitement 
            /// </summary>
            public Nullable<decimal> treatedRate;
            #endregion Members


            #region Constructors
            /// <summary>
            /// Constructor où observedRate et  treatedRate sont obtenus par lecture des table  EVENT (treatedRate) et EVENTDET (observedRate)
            /// </summary>
            /// <param name="pAccrualDate"></param>
            /// <param name="pCommonValProcess"></param>
            /// <param name="pRowReset"></param>
            public CalculationInterestLegResetEvent(DateTime pDtBusiness, EventsValProcessBase pEventsValProcess, DataRow pRowReset)
                : base(pDtBusiness, pEventsValProcess, pRowReset)
            {
                DataRow rowDetail = pEventsValProcess.GetRowDetail(Convert.ToInt32(pRowReset["IDE"]));
                if ((null != rowDetail) && (false == Convert.IsDBNull(rowDetail["RATE"])))
                    observedRate = Convert.ToDecimal(rowDetail["RATE"]);
                if (false == Convert.IsDBNull(pRowReset["VALORISATION"]))
                    treatedRate = Convert.ToDecimal(pRowReset["VALORISATION"]);
            }
            /// <summary>
            /// Constructor où
            /// </summary>
            /// <param name="pAccrualDate"></param>
            /// <param name="pCommonValProcess"></param>
            /// <param name="pRowReset"></param>
            /// <param name="pIdAsset"></param>
            /// <param name="pIdAsset2"></param>
            /// <param name="pRateCutOffDate"></param>
            // EG 20150306 [POC-BERKELEY] : Refactoring Gestion des erreurs
            // EG 20150311 [POC-BERKELEY] : Lecture d'un prix Bid/ASk si FDA sur CFD FOREX
            // EG 20180502 Analyse du code Correction [CA2200]
            // EG 20190114 Add detail to ProcessLog Refactoring
            // EG 20190716 [VCL : FixedIncome] Upd GetQuoteLock
            public CalculationInterestLegResetEvent(DateTime pDtBusiness, EventsValProcessBase pEventsValProcess, DataRow pRowReset, int pIdAsset)
                : base(pDtBusiness, pEventsValProcess, pRowReset, pIdAsset)
            {
                SystemMSGInfo quoteMsgInfo = null;
                try
                {
                    #region Process
                    DataRow[] rowSelfAverages = pRowReset.GetChildRows(m_EventsValProcess.DsEvents.ChildEvent);
                    if (ArrFunc.IsEmpty(rowSelfAverages))
                    {
                        #region Observated Rate
                        // Lecture d'un prix Bid/ASk si FDA sur CFD FOREX
                        Quote _quote = pEventsValProcess.Process.ProcessCacheContainer.GetQuoteLock(m_IdAsset, m_ObservedRateDate, string.Empty,
                            pEventsValProcess.FundingRateQuotationSide, Cst.UnderlyingAsset.RateIndex, new KeyQuoteAdditional(), ref quoteMsgInfo);


                        if ((null != quoteMsgInfo) && (quoteMsgInfo.processState.CodeReturn != Cst.ErrLevel.SUCCESS))
                        {
                            UpdatingRow(pRowReset);
                            quoteMsgInfo.processState = new ProcessState(ProcessStateTools.StatusWarningEnum, quoteMsgInfo.processState.CodeReturn);
                            throw new SpheresException2(quoteMsgInfo.processState);
                        }

                        m_RowAssets[0]["QUOTESIDE"] = _quote.QuoteSide;
                        m_RowAssets[0]["IDMARKETENV"] = _quote.idMarketEnv;
                        m_RowAssets[0]["IDVALSCENARIO"] = _quote.idValScenario;
                        if (_quote.valueSpecified)
                            observedRate = _quote.value;
                        RateTreatement();
                        UpdatingRow(pRowReset);
                        #endregion Observated Rate
                    }
                    else
                    {
                        #region SelfCompounding
                        // TBD
                        #endregion SelfCompounding
                    }
                    CommonValFunc.SetRowCalculated(pRowReset);
                    m_EventsValProcess.SetRowStatus(pRowReset, Tuning.TuningOutputTypeEnum.OES);
                    #endregion Process
                }

                catch (SpheresException2 ex)
                {
                    bool isThrow = true;
                    if (false == ProcessStateTools.IsCodeReturnUndefined(ex.ProcessState.CodeReturn))
                    {
                        if (isThrow && (null != quoteMsgInfo))
                        {
                            // FI 20200623 [XXXXX] SetErrorWarning
                            m_EventsValProcess.Process.ProcessState.SetErrorWarning(quoteMsgInfo.processState.Status);
                            Logger.Log(quoteMsgInfo.ToLoggerData(0));

                            throw new SpheresException2(MethodInfo.GetCurrentMethod().Name, "SYS-05160", quoteMsgInfo.processState,
                                LogTools.IdentifierAndId(m_EventsValProcess.EventsValMQueue.GetStringValueIdInfoByKey("identifier"), m_EventsValProcess.EventsValMQueue.id));
                        }
                    }
                    else
                    {
                        throw;
                    }
                }
                catch (Exception)
                {
                    CommonValProcessBase.ResetRowCalculated(pRowReset);
                    m_EventsValProcess.SetRowStatus(pRowReset, Tuning.TuningOutputTypeEnum.OEE);
                    throw;
                }
            }
            #endregion Constructors
            #region Methods
            #region RateTreatement
            private void RateTreatement()
            {
                treatedRate = observedRate;
                if (m_RateTreatmentSpecified && observedRate.HasValue)
                    treatedRate = CommonValFunc.TreatedRate(m_EventsValProcess.ProductBase, m_RateTreatment, observedRate.Value, m_StartDate, m_EndDate, m_PaymentFrequency);
            }
            #endregion RateTreatement
            #region UpdatingRow
            private void UpdatingRow(DataRow pRow)
            {
                pRow["VALORISATION"] = (treatedRate ?? Convert.DBNull);
                pRow["UNITTYPE"] = UnitTypeEnum.Rate.ToString();
                pRow["VALORISATIONSYS"] = (treatedRate ?? Convert.DBNull);
                pRow["UNITTYPESYS"] = UnitTypeEnum.Rate.ToString();
                DataRow rowDetail = m_EventsValProcess.GetRowDetail(Convert.ToInt32(pRow["IDE"]));
                if (null != rowDetail)
                    rowDetail["RATE"] = (observedRate ?? Convert.DBNull);
            }
            #endregion UpdatingRow
            #endregion Methods
        }
        #endregion CalculationInterestLegResetEvent

        #region Initialize
        /// <summary>
        /// Alimente les membres m_ReturnSwapContainer,_buyer,_bookBuyer,_seller,_bookSeller de la classe
        /// </summary>
        /// EG 20170510 [23153] Refactoring
        // EG 20190613 [24683] Use slaveDbTransaction
        // EG 20191115 [25077] RDBMS : New version of Trades tables architecture (TRADESTSYS merge to TRADE, NEW TABLE TRADEXML)
        private void Initialize()
        {
            // EG 20170510 [23153]
            m_ReturnSwapContainer = new ReturnSwapContainer((IReturnSwap)m_CurrentProduct, TradeLibrary.DataDocument);

            m_EventsValProcess.ProcessCacheContainer.SetAsset(m_ReturnSwapContainer.MainReturnLeg.Second);
            m_EventsValProcess.ProcessCacheContainer.SetAsset(m_ReturnSwapContainer.MainInterestLeg.Second);

            string tradeStBusiness = DsTrade.DtTrade.Rows[0]["IDSTBUSINESS"].ToString();
            m_ReturnSwapContainer.InitRptSide(m_CS, (Cst.StatusBusiness.ALLOC.ToString() == tradeStBusiness));

            // Buyer - Seller
            m_Buyer = m_ReturnSwapContainer.GetBuyer();
            m_Seller = m_ReturnSwapContainer.GetSeller();
            if (null == m_Buyer)
                throw new NotSupportedException("buyer is not Found");
            if (null == m_Seller)
                throw new NotSupportedException("seller is not Found");

            m_BookBuyer = m_ReturnSwapContainer.DataDocument.GetOTCmlId_Book(m_Buyer.Id);
            m_BookSeller = m_ReturnSwapContainer.DataDocument.GetOTCmlId_Book(m_Seller.Id);


            // EG 20170510 [23153]
            int idEntity;
            if ((null != m_Quote) && m_Quote.eodComplementSpecified)
            {
                m_IsPosKeeping_BookDealer = m_Quote.eodComplement.isPosKeeping_BookDealer;
                idEntity = m_Quote.eodComplement.idAEntity;
            }
            else
            {
                m_IsPosKeeping_BookDealer = PosKeepingTools.IsPosKeepingBookDealer(m_CS, Process.SlaveDbTransaction, m_DsTrade.IdT, Cst.ProductGProduct_OTC);
                idEntity = TradeLibrary.DataDocument.GetFirstEntity(CSTools.SetCacheOn(m_CS), Process.SlaveDbTransaction);
            }

            if (m_ReturnSwapContainer.IdA_Custodian.HasValue)
                m_EntityMarketInfo = m_EventsValProcess.ProcessCacheContainer.GetEntityMarketLock(idEntity, m_ReturnSwapContainer.IdM.Value, m_ReturnSwapContainer.IdA_Custodian);
        }
        #endregion Initialize

        #region IsRowMustBeCalculate
        public override bool IsRowMustBeCalculate(DataRow pRow)
        {
            string eventCode = pRow["EVENTCODE"].ToString();
            string eventType = pRow["EVENTTYPE"].ToString();

            if (null != m_EventsValMQueue.quote)
            {
                if (IsQuote_RateIndex || IsQuote_Equity || IsQuote_Index)
                {
                    if (EventCodeFunc.IsReset(eventCode) || EventTypeFunc.IsReturnLegAmount(eventType))
                    {
                        if (IsRowHasFixingEvent(pRow))
                            return true;
                        else
                            return IsRowHasChildrens(pRow);
                    }
                    else
                        return IsRowHasChildrens(pRow);

                }
            }
            return true;
        }
        #endregion IsRowMustBeCalculate

        #region Calculation_LPC_AMT
        /// <summary>
        /// Génération des évènement VariationMargin, UnrealizedMargin, ReturnSwapAmount, MarginRequirement
        /// </summary>
        /// EG 20150306 [POC-BERKELEY] : Refactoring Evenement RSA conditionné
        /// EG 20150306 [POC-BERKELEY] : New IsMargining
        /// EG 20170412 [23081] Gestion  dbTransaction et SlaveDbTransaction
        /// EG 20170510 [23153] Refactoring
        // EG 20180307 [23769] Gestion dbTransaction
        // EG 20190613 [24683] Upd Calling WriteClosingAmount
        // EG 20190925 Retour arrière CashFlows (Mise en veille de l'appel à )EndOfDayWriteClosingAmountGen
        private void Calculation_LPC_AMT(RTSQuoteInfo pQuote, RTSQuoteInfo pQuotePrev)
        {
            bool isCalculation = (m_ReturnSwapContainer.MainReturnLeg.First.IsDailyPeriod) ||
                                 (null != GetRowAmount(EventTypeFunc.ReturnSwapAmount, EventClassFunc.Recognition, 
                                 m_Quote.QuoteTiming.Value, pQuote.dtBusiness));

            if (isCalculation)
            {
                DataRow rowEventAMT = GetRowAmountGroup();
                m_ParamInstrumentNo.Value = Convert.ToInt32(rowEventAMT["INSTRUMENTNO"]);
                m_ParamStreamNo.Value = Convert.ToInt32(rowEventAMT["STREAMNO"]);

                Pair<string, int> quotedCurrency = new Pair<string, int>
                {
                    First = m_ReturnSwapContainer.UnderlyerCurrency,
                    Second = Tools.GetQuotedCurrencyFactor(CSTools.SetCacheOn(m_CS), null, m_ReturnSwapContainer.UnderlyerCurrency)
                };

                // EG 20170510 [21153] Alimentation d'une liste des événements calculés (new, update or delete) 
                List<VAL_Event> lstEvents = new List<VAL_Event>()
                {
                    PrepareClosingAmountGen(rowEventAMT, EventTypeFunc.UnrealizedMargin, quotedCurrency, pQuote, pQuotePrev),
                    PrepareClosingAmountGen(rowEventAMT, EventTypeFunc.MarketValue, quotedCurrency, pQuote, pQuotePrev)
                };

                if (false == m_IsPosKeeping_BookDealer)
                    lstEvents.Add(PrepareClosingAmountGen(rowEventAMT, EventTypeFunc.ReturnSwapAmount, quotedCurrency, pQuote, pQuotePrev));

                // EG 20150306 New m_IsMargining
                if (m_IsMargining)
                {
                    lstEvents.Add(PrepareClosingAmountGen(rowEventAMT, EventTypeFunc.MarginRequirement, quotedCurrency, pQuote, pQuotePrev));
                    lstEvents.Add(PrepareClosingAmountGen(rowEventAMT, EventTypeFunc.TotalMargin, quotedCurrency, pQuote, pQuotePrev));
                }

                WriteClosingAmountGen(lstEvents, pQuote.dtBusiness);
            }
        }
        #endregion Calculation_LPC_AMT


        #region PrepareClosingAmountGen
        /// <summary>
        /// Calcul des montants
        /// </summary>
        /// EG 20170510 [23153] New
        // EG 20190114 Add detail to ProcessLog Refactoring
        // EG 20190924 Insert Duplicate key (ReturnLeg|InterestLeg) = LA maj InterestLeg rejoue les Inserts ReturnLeg
        // EG 20190925 Retour arrière CashFlows (Mise en veille de l'appel à )EndOfDayWriteClosingAmountGen
        private VAL_Event PrepareClosingAmountGen(DataRow pRowEventAMT, string pEventType, Pair<string, int> pQuotedCurrency, RTSQuoteInfo pQuote, RTSQuoteInfo pQuotePrev)
        {
            Pair<Nullable<decimal>, string> closingAmount = new Pair<Nullable<decimal>, string>(null, string.Empty);

            VAL_Event @event = new VAL_Event(); 

            string eventCodeLink = EventCodeLink(EventTypeFunc.Amounts, pEventType, pQuote.quote.QuoteTiming.Value);

            @event.Value = GetRowAmount(pEventType, EventClassFunc.Recognition, m_Quote.QuoteTiming.Value, pQuote.dtBusiness);
            @event.IsNewRow = (null == @event.Value);

            bool isOkToGenerate = IsEndOfDayProcess || ((false == @event.IsNewRow) && (false == IsEndOfDayProcess));
            if (isOkToGenerate)
            {
                Pair<int, Nullable<int>> payer = new Pair<int, Nullable<int>>();
                Pair<int, Nullable<int>> receiver = new Pair<int, Nullable<int>>();

                if (@event.IsNewRow)
                {
                    @event.Value = NewRowEvent2(pRowEventAMT, eventCodeLink, pEventType, pQuote.dtBusiness, pQuote.dtBusiness, m_EventsValProcess.AppInstance);
                    @event.ClassREC = NewRowEventClass(-1, EventClassFunc.Recognition, pQuote.dtBusiness, false);
                    if (EventTypeFunc.IsUnrealizedMargin(pEventType) || EventTypeFunc.IsMarginRequirement(pEventType) || EventTypeFunc.IsMarketValue(pEventType))
                        @event.ClassVAL = NewRowEventClass(-1, EventClassFunc.ValueDate, pQuote.dtBusiness, false);

                    @event.Detail = NewRowEventDet(@event.Value);
                }
                else
                {
                    @event.Detail = GetRowEventDetail(Convert.ToInt32(@event.Value["IDE"]));
                }

                Nullable<decimal> quote = null;
                Nullable<decimal> quote100 = null;
                if (pQuote.quote.valueSpecified)
                {
                    quote = pQuote.quote.value;
                    quote100 = pQuote.quote.value;
                }

                decimal _initialNetPrice = m_ReturnSwapContainer.MainInitialNetPrice.Value;
                decimal multiplier = Multiplier;
                bool isTradeDay = (m_ReturnSwapContainer.ClearingBusinessDate.Date == pQuote.dtBusiness);

                @event.Detail["DTACTION"] = pQuote.dtQuote;
                @event.Detail["QUOTETIMING"] = pQuote.quote.quoteTiming;
                @event.Detail["QUOTEPRICE"] = quote ?? Convert.DBNull;
                @event.Detail["QUOTEPRICE100"] = quote100 ?? Convert.DBNull;
                @event.Detail["CONTRACTMULTIPLIER"] = multiplier;
                @event.Detail["QUOTEDELTA"] = DBNull.Value;

                @event.Qty = m_PosAvailableQuantity;

                if (EventTypeFunc.IsUnrealizedMargin(pEventType))
                {
                    #region UnrealizedMargin
                    closingAmount = SetUnrealizedMargin(null, @event.Qty, multiplier, pQuotedCurrency.First, quote100, _initialNetPrice, ref payer, ref receiver);
                    @event.Detail["PRICE"] = _initialNetPrice;
                    @event.Detail["PRICE100"] = _initialNetPrice;
                    #endregion UnrealizedMargin
                }
                else if (EventTypeFunc.IsReturnSwapAmount(pEventType))
                {
                    #region ReturnSwapAmount
                    Nullable<decimal> _notionalAmount = @event.Qty * _initialNetPrice * multiplier;

                    if (isTradeDay)
                    {
                        if (quote100.HasValue)
                        {
                            closingAmount = Tools.ConvertToQuotedCurrency(CSTools.SetCacheOn(m_CS), null, 
                                    new Pair<Nullable<decimal>, string>(((quote100.Value - _initialNetPrice) / _initialNetPrice) * _notionalAmount, pQuotedCurrency.First));
                        }
                    }
                    else
                    {
                        if (m_ReturnSwapContainer.IsMainNotionalReset)
                            _notionalAmount = GetAmount(pQuotePrev.dtBusiness, EventTypeFunc.MarketValue);

                        string assetIdentifierLog = LogTools.IdentifierAndId(m_Quote.idAsset_Identifier, m_Quote.idAsset);

                        // Le cours veille est lu dans le cache
                        pQuotePrev.SetQuote(this);
                        Quote currentQuote = pQuotePrev.quote;

                        if (((null == currentQuote) || (false == currentQuote.valueSpecified)) && (0 < @event.Qty))
                        {
                            // Previous VMG not found => throw exception
                            #region Log error message
                            ProcessState _processState = new ProcessState(ProcessStateTools.StatusErrorEnum);
                            if (m_Quote.isEOD)
                            {
                                _processState.Status = ProcessStateTools.StatusEnum.WARNING;
                                _processState.CodeReturn = ProcessStateTools.CodeReturnQuoteNotFoundEnum;
                                m_EventsValProcess.ProcessState.SetProcessState(_processState);
                            }
                            string currentQuote_time = (null == currentQuote) || (false == currentQuote.valueSpecified) ? "n/a" : DtFunc.DateTimeToString(currentQuote.time, DtFunc.FmtDateTime);

                            // FI 20200623 [XXXXX] SetErrorWarning
                            m_EventsValProcess.ProcessState.SetErrorWarning(_processState.Status);

                            Logger.Log(new LoggerData(LoggerTools.StatusToLogLevelEnum(_processState.Status), new SysMsgCode(SysCodeEnum.SYS, 5158), 2,
                                new LogParam(LogTools.IdentifierAndId(m_EventsValMQueue.GetStringValueIdInfoByKey("identifier"), m_DsTrade.IdT)),
                                new LogParam(assetIdentifierLog),
                                new LogParam(currentQuote_time),
                                new LogParam(pEventType)));

                            throw new SpheresException2(_processState);
                            #endregion Log error message
                        }

                        decimal quoteVeil = pQuotePrev.quote.value;
                        decimal quoteVeil100 = quoteVeil;
                        if (quote100.HasValue && _notionalAmount.HasValue)
                        {
                            closingAmount = Tools.ConvertToQuotedCurrency(CSTools.SetCacheOn(m_CS), null, 
                                    new Pair<Nullable<decimal>, string>(((quote100.Value - quoteVeil100) / quoteVeil100) * _notionalAmount, pQuotedCurrency.First));
                        }
                    }

                    @event.Detail["NOTIONALAMOUNT"] = _notionalAmount;
                    bool amountValuatedAndPositive = (closingAmount.First.HasValue) && (0 < closingAmount.First.Value);
                    payer.First = amountValuatedAndPositive ? m_Buyer.OTCmlId : m_Seller.OTCmlId;
                    payer.Second = amountValuatedAndPositive ? m_BookBuyer : m_BookSeller;
                    receiver.First = amountValuatedAndPositive ? m_Seller.OTCmlId : m_Buyer.OTCmlId;
                    receiver.Second = amountValuatedAndPositive ? m_BookSeller : m_BookBuyer;
                    #endregion ReturnSwapAmount
                }
                else if (EventTypeFunc.IsMarketValue(pEventType))
                {
                    #region MarketValue
                    closingAmount = SetMarketValue(null, @event.Qty, multiplier, pQuotedCurrency.First, quote100, ref payer, ref receiver);
                    #endregion MarketValue
                }
                else if (EventTypeFunc.IsMarginRequirement(pEventType))
                {
                    #region MarginRequirement
                    closingAmount = SetMarginRequirement(null, m_ReturnSwapContainer, @event.Qty, multiplier, pQuotedCurrency.First, pQuote.dtBusiness, quote100, ref payer, ref receiver);
                    #endregion MarginRequirement
                }
                else if (EventTypeFunc.IsTotalMargin(pEventType))
                {
                    #region TotalMargin
                    if (false == isTradeDay)
                        pQuotePrev.SetQuote(this);
                    closingAmount = SetTotalMargin(null, isTradeDay, @event.Qty, _initialNetPrice, multiplier, pQuotedCurrency.First,
                        pQuote.dtBusiness, quote100, pQuotePrev.dtBusiness, (isTradeDay ? 0 : pQuotePrev.quote.value), ref payer, ref receiver);
                    #endregion TotalMargin
                }

                @event.ClosingAmount = closingAmount.First;
                CommonValFunc.SetPayerReceiver(@event.Value, payer.First, payer.Second, receiver.First, receiver.Second);
                // EG 20170522 set Currency
                @event.Currency = closingAmount.Second;

                // EG 20190924 Insert Duplicate key
                //@event.SetRowEventClosingAmountGen(IsEndOfDayProcess ? null : m_DsEvents, pQuotedCurrency.Second, false);
                @event.SetRowEventClosingAmountGen(m_DsEvents, pQuotedCurrency.Second, false);
            }
            return @event;
        }
        #endregion PrepareClosingAmountGen
        
        #region ExistEventClass
        private bool ExistEventClass(DataRow pRow, string pEventClass, DateTime pDtEvent)
        {
            IEnumerable<DataRow> _rowsEventClass = pRow.GetChildRows(DsEvents.ChildEventClass)
                .Where(_rowClass => (_rowClass["EVENTCLASS"].ToString() == pEventClass) && (Convert.ToDateTime(_rowClass["DTEVENT"]) == pDtEvent));
            return (0 < _rowsEventClass.Count());
        }
        #endregion ExistEventClass

        #region GetCurrentMarginRatio
        // EG 20160404 Migration vs2013
//        private decimal GetCurrentMarginRatio(DateTime pDate)
//        {
//            decimal _returnSwapAmount = 0;
//            DataRow[] _rows = DsEvents.DtEvent.Select(StrFunc.AppendFormat(@"IDT = {0} and EVENTTYPE = '{1}' and EVENTCODE = '{2}' 
//            and INSTRUMENTNO = {3} and DTSTARTADJ <= '{4}' and '{4}' < DTENDADJ",
//            m_DsTrade.IdT, EventTypeFunc.MarginRequirementRatio.ToString(), EventCodeFunc.LinkedProductPayment.ToString(),
//            StrFunc.GetSuffixNumeric2(m_CurrentProduct.productBase.id), DtFunc.DateTimeToStringDateISO(pDate)));
//            if (ArrFunc.IsFilled(_rows))
//                _returnSwapAmount = Convert.ToDecimal(_rows.First()["VALORISATION"]);
//            return _returnSwapAmount;
//        }
        #endregion GetCurrentMarginRatio
        #region GetRowFundingAmount
        /// <summary>
        ///  Retourne les Evènements tels que EVENTTYPE = 'FDA' et pour lesquels la date REC = {pDate}
        /// </summary>
        /// <param name="pDate"></param>
        /// <returns></returns>
        public IEnumerable<DataRow> GetRowFundingAmount(DateTime pDate)
        {
            IEnumerable<DataRow> _rowsCandidate = null;
            DataRow[] _rows = DsEvents.DtEvent.Select(StrFunc.AppendFormat(@"IDT = {0} and EVENTTYPE = '{1}' and INSTRUMENTNO = {2}",
            m_DsTrade.IdT, EventTypeFunc.FundingAmount, StrFunc.GetSuffixNumeric2(m_CurrentProduct.ProductBase.Id)));
            if (ArrFunc.IsFilled(_rows))
                _rowsCandidate = _rows.Where(_rowEvent => ExistEventClass(_rowEvent, EventClassFunc.Recognition, pDate));
            return _rowsCandidate;
        }
        #endregion GetRowFundingAmount
        #region GetRowInterestLeg
        public DataRow GetRowMainInterestLeg()
        {
            DataRow _rowsCandidate = null;
            DataRow[] _rows = DsEvents.DtEvent.Select(StrFunc.AppendFormat(@"IDT = {0} and EVENTCODE = '{1}' and INSTRUMENTNO = {2}",
            m_DsTrade.IdT, EventCodeFunc.InterestLeg, StrFunc.GetSuffixNumeric2(m_CurrentProduct.ProductBase.Id)));
            if (ArrFunc.IsFilled(_rows))
                _rowsCandidate = _rows[0];
            return _rowsCandidate;
        }
        #endregion GetRowFundingAmount

        #region RemoveClosingAmountGen
        private void RemoveClosingAmountGen(DateTime pDate)
        {
        }
        #endregion RemoveClosingAmountGen
        #region Valorize
        // EG 20150306 [POC-BERKELEY] : New Isfunding
        // EG 20180502 Analyse du code Correction [CA2214]
        // RD 20200911 [25475] Add try catch in order to Log the Exception
        // EG 20231127 [WI749] Implementation Return Swap : UPD
        public override Cst.ErrLevel Valorize()
        {
            Cst.ErrLevel ret = Cst.ErrLevel.SUCCESS;
            if (m_tradeLibrary.DataDocument.CurrentProduct.IsStrategy)
                return ret;

            // EG 20231115 GLOP TEMPORAIRE A REVOIR *****************************
            if (m_IsFungible)
                ret = ValorizeContractForDifference();
            else
                ret = ValorizeReturnSwap();

            return ret;
        }
        // EG 20231127 [WI749] Implementation Return Swap : New
        private Cst.ErrLevel ValorizeContractForDifference()
        {
            Cst.ErrLevel ret = Cst.ErrLevel.SUCCESS;
            try
            {
                RTSQuoteInfo _quoteInfo = new RTSQuoteInfo(this, NextPreviousEnum.None);
                if ((null != _quoteInfo) && (_quoteInfo.rowState == DataRowState.Deleted))
                {
                    _quoteInfo.SetQuote(this);
                    RemoveClosingAmountGen(_quoteInfo.dtBusiness);
                }

                if (_quoteInfo.rowState != DataRowState.Deleted)
                {
                    bool isFirstQuotationFound = true;
                    if (false == IsEndOfDayProcess)
                    {
                        //Spheres® vérifie que la date de cotation ne correspond pas à un jour férié
                        _quoteInfo.BusinessDayControl(this);
                        isFirstQuotationFound = _quoteInfo.SetQuote(this);
                    }
                    else
                    {
                        _quoteInfo.InitQuote(this);
                    }

                    bool isCalculation = (false == _quoteInfo.quote.QuoteTiming.HasValue) || (QuoteTimingEnum.Close == _quoteInfo.quote.QuoteTiming.Value);

                    if (isCalculation)
                    {
                        // EG 20170510 [23153] 
                        if (m_IsFunding || m_IsMargining)
                            InitalizeFeeRequest();

                        if (IsQuoteReturnLeg)
                            ret = CalculationReturnLeg(_quoteInfo);
                        if ((Cst.ErrLevel.SUCCESS == ret) && m_IsFunding)
                            ret = CalculationInterestLeg(_quoteInfo.dtBusiness);
                    }
                }
            }
            catch (SpheresException2 ex)
            {
                Logger.Log(new LoggerData(ex));
                throw ex;
            }
            catch (Exception ex)
            {
                SpheresException2 sphEx = new SpheresException2(MethodInfo.GetCurrentMethod().Name, ex);
                Logger.Log(new LoggerData(sphEx));
                throw sphEx;
            }

            return ret;
        }
        #endregion Valorize


        #region SetRowAssetToFundingAmountOrReset
        public override void SetRowAssetToFundingAmountOrReset(DataRow pRow, IInterestCalculation pInterestCalculation)
        {
            int idE = Convert.ToInt32(pRow["IDE"]);
            DataRow[] rowAssets = GetRowAsset(idE);
            DataRow rowAsset;
            if (ArrFunc.IsEmpty(rowAssets))
            {
                rowAsset = NewRowEventAsset(GetRowAsset(Convert.ToInt32(pRow["IDE_EVENT"])).FirstOrDefault(), idE);
                m_DsEvents.DtEventAsset.Rows.Add(rowAsset);
            }
            rowAsset = GetRowAsset(idE).FirstOrDefault();

            SQL_AssetBase _asset = pInterestCalculation.SqlAsset;
            rowAsset.BeginEdit();
            rowAsset["IDASSET"] = _asset.Id;
            rowAsset["IDC"] = _asset.IdC;
            rowAsset["IDM"] = (_asset.IdM > 0) ? _asset.IdM : Convert.DBNull;
            rowAsset["ASSETCATEGORY"] = _asset.AssetCategory;
            if (_asset is SQL_AssetRateIndex)
            {
                SQL_AssetRateIndex _assetRateIndex = _asset as SQL_AssetRateIndex;
                rowAsset["IDBC"] = _assetRateIndex.Idx_IdBc;
            }
            rowAsset.EndEdit();
        }
        #endregion SetRowAssetToFundingAmountOrReset
        #region SetRowDetailToFundingAmount
        // EG 20150921 Test paymentPeriods
        public override void SetRowDetailToFundingAmount(int pIdE, Pair<IInterestLeg, IInterestCalculation> pCurrentInterestLeg)
        {
            if (ArrFunc.IsFilled(pCurrentInterestLeg.First.Efs_InterestLeg.paymentPeriods))
            {
                DataRow rowDetails = GetRowDetail(pIdE);
                EFS_InterestLegPaymentDate paymentDate = pCurrentInterestLeg.First.Efs_InterestLeg.paymentPeriods.First();
                rowDetails.BeginEdit();
                rowDetails["DCF"] = pCurrentInterestLeg.Second.DayCountFraction.ToString();
                rowDetails["MULTIPLIER"] = paymentDate.multiplierSpecified ? paymentDate.Multiplier.DecValue : Convert.DBNull;
                rowDetails["SPREAD"] = paymentDate.spreadSpecified ? paymentDate.Spread.DecValue : Convert.DBNull;
                if (pCurrentInterestLeg.Second.FixedRateSpecified)
                    rowDetails["RATE"] = pCurrentInterestLeg.Second.FixedRate.DecValue;
                rowDetails.EndEdit();
            }
        }
        #endregion SetRowDetailToFundingAmount

        /* NEW NEW NEW  = B O R R O W I N G */

        #region CalculationInterestLeg
        /// <summary>
        /// Calcul des montants INTERESTLEG (FDA et BWA)
        /// </summary>
        /// <returns></returns>
        /// EG 20150306 [POC] : Refactoring Gestion des erreurs
        /// EG 20150309 [POC] : Lecture Funding sur référentiel
        /// EG 20150319 [POC] : Lecture Borrowing sur référentiel
        private Cst.ErrLevel CalculationInterestLeg(DateTime pDtBusiness)
        {
            Cst.ErrLevel ret = PrepareDailyInterestLeg(Cst.FundingType.Funding, pDtBusiness);
            if (Cst.ErrLevel.SUCCESS == ret)
                ret = PrepareDailyInterestLeg(Cst.FundingType.Borrowing, pDtBusiness);
            return ret;
        }
        #endregion CalculationInterestLeg
        #region PrepareDailyInterestLeg
        /// EG 20170412 [23081] Gestion  dbTransaction et SlaveDbTransaction
        /// EG 20170510 [23153] Refactoring
        // EG 20180205 [23769] Upd DataDocumentContainer parameter (substitution to the static class EFS_CURRENT)  
        // EG 20180502 Analyse du code Correction [CA2200]
        private Cst.ErrLevel PrepareDailyInterestLeg(Cst.FundingType pFundingType, DateTime pDtBusiness)
        {
            Cst.ErrLevel ret = Cst.ErrLevel.SUCCESS;
            ArrayList alSpheresException = new ArrayList();

            Pair<IReturnLeg, IReturnLegMainUnderlyer> _returnLeg = m_ReturnSwapContainer.MainReturnLeg;
            Pair<IInterestLeg, IInterestCalculation> _currentInterestLeg = m_ReturnSwapContainer.MainInterestLeg;
            // Lecture dans le référentiel du Funding|Borrowing
            _currentInterestLeg.Second = GetInterestLegRate(pFundingType);

            if (null != _currentInterestLeg.Second)
            {
                _currentInterestLeg.First.Efs_InterestLeg = new EFS_InterestLeg(m_CS, TradeLibrary.DataDocument);
                _currentInterestLeg.First.Efs_InterestLeg.InitMembers(_currentInterestLeg.First, _currentInterestLeg.First.Notional);
                IEnumerable<DataRow> _rowsFundingBorrowing = PrepareDailyInterestLegRow(pFundingType, _returnLeg.First, _currentInterestLeg, pDtBusiness);
                if (null != _rowsFundingBorrowing)
                {

                    IDbTransaction dbTransaction = null;
                    if (null != SlaveDbTransaction)
                        dbTransaction = SlaveDbTransaction;

                    bool isException = false;
                    try
                    {
                        if (null == SlaveDbTransaction)
                            dbTransaction = DataHelper.BeginTran(m_EventsValProcess.Cs);

                        foreach (DataRow _rowAmount in _rowsFundingBorrowing)
                        {
                            SetRowAssetToFundingAmountOrReset(_rowAmount, _currentInterestLeg.Second);
                            SetRowDetailToFundingAmount(Convert.ToInt32(_rowAmount["IDE"]), _currentInterestLeg);

                            bool isError = false;
                            m_ParamInstrumentNo.Value = Convert.ToInt32(_rowAmount["INSTRUMENTNO"]);
                            m_ParamStreamNo.Value = Convert.ToInt32(_rowAmount["STREAMNO"]);
                            int idE = Convert.ToInt32(_rowAmount["IDE"]);

                            bool isRowMustBeCalculate = IsRowMustBeCalculate(_rowAmount);
                            if (isRowMustBeCalculate)
                            {
                                try
                                {
                                    // EG 20170510 [23153]
                                    if (0 < m_PosAvailableQuantity)
                                    {
                                        _rowAmount.BeginEdit();
                                        Parameters.Add(m_CS, m_tradeLibrary, _rowAmount);
                                        CommonValFunc.SetRowCalculated(_rowAmount);
                                        DataRow _rowINL = _rowAmount.GetParentRow(DsEvents.ChildEvent);

                                        CalculationInterestLegEvent interestLegEvent = new CalculationInterestLegEvent(pDtBusiness, this, _rowINL, _rowAmount,
                                            _returnLeg, _currentInterestLeg, m_PosAvailableQuantity);
                                    }
                                }
                                catch (SpheresException2 ex)
                                {
                                    // EG 20150305 New
                                    if (ProcessStateTools.IsStatusErrorWarning(ex.ProcessState.Status))
                                    {
                                        alSpheresException.Add(ex);
                                        ret = (ProcessStateTools.IsStatusError(ex.ProcessState.Status) ? Cst.ErrLevel.FAILURE : Cst.ErrLevel.FAILUREWARNING);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    // EG 20150305 New
                                    ret = Cst.ErrLevel.FAILURE;
                                    alSpheresException.Add(new SpheresException2(MethodInfo.GetCurrentMethod().Name, ex));
                                }
                                finally
                                {
                                    if (0 == m_PosAvailableQuantity)
                                    {
                                        _rowAmount.Delete();
                                        DsEvents.Update(dbTransaction, IsEndOfDayProcess);
                                    }
                                    else
                                    {
                                        _rowAmount.EndEdit();
                                        Update(dbTransaction, idE, isError);
                                    }
                                }
                            }
                        }
                        if (null == SlaveDbTransaction)
                        {
                            DataHelper.CommitTran(dbTransaction);
                            dbTransaction = null;
                        }
                    }
                    catch (Exception)
                    {
                        isException = true;
                        throw;
                    }
                    finally
                    {
                        if ((null != dbTransaction) && (null == SlaveDbTransaction) && isException)
                        {
                            try { DataHelper.RollbackTran(dbTransaction); }
                            catch { }
                        }
                    }
                }

                if (ArrFunc.IsFilled(alSpheresException))
                {
                    m_EventsValProcess.ProcessState.SetProcessState(new ProcessState(ProcessStateTools.StatusEnum.ERROR, ret));
                    
                    foreach (SpheresException2 ex in alSpheresException)
                    {
                        Logger.Log(new LoggerData(ex));
                    }
                }
            }
            return ret;
        }
        #endregion PrepareDailyInterestLeg
        #region PrepareDailyInterestLegRow
        private IEnumerable<DataRow> PrepareDailyInterestLegRow(Cst.FundingType pFundingType,
            IReturnLeg pReturnLeg, Pair<IInterestLeg, IInterestCalculation> pInterestLeg, DateTime pDtBusiness)
        {
            IEnumerable<DataRow> _rowsAmount = GetRowInterestLegAmount(pFundingType, pDtBusiness);
            bool isNewRowAmount = (((null == _rowsAmount) || (0 == _rowsAmount.Count())) && IsEndOfDayProcess);
            if (pReturnLeg.IsDailyPeriod && pInterestLeg.First.IsPeriodRelativeToReturnLeg)
            {
                if (0 < m_PosAvailableQuantity)
                {
                    // La date EffectiveDate est écrasée par la date de traitement en cours (DtBusiness)
                    if (pReturnLeg.EffectiveDate.AdjustableDateSpecified)
                    {
                        DateTime _savEffectiveDate = pReturnLeg.EffectiveDate.AdjustableDate.UnadjustedDate.DateValue;
                        pReturnLeg.EffectiveDate.AdjustableDate.UnadjustedDate.DateValue = pDtBusiness;
                        if (isNewRowAmount)
                            CreateDailyRowInterestLegAmount(pFundingType, pInterestLeg);
                        else
                            pInterestLeg.First.Efs_InterestLeg.CalcDailyPeriod(pInterestLeg);

                        pReturnLeg.EffectiveDate.AdjustableDate.UnadjustedDate.DateValue = _savEffectiveDate;

                    }
                    else if (pReturnLeg.EffectiveDate.RelativeDateSpecified)
                    {
                        pReturnLeg.EffectiveDate.RelativeDateSpecified = false;
                        pReturnLeg.EffectiveDate.AdjustableDateSpecified = true;
                        pReturnLeg.EffectiveDate.AdjustableDate =
                            m_ReturnSwapContainer.Product.ProductBase.CreateAdjustableDate(pDtBusiness, BusinessDayConventionEnum.NotApplicable, null);
                        if (isNewRowAmount)
                            CreateDailyRowInterestLegAmount(pFundingType, pInterestLeg);
                        else
                            pInterestLeg.First.Efs_InterestLeg.CalcDailyPeriod(pInterestLeg);

                        pReturnLeg.EffectiveDate.RelativeDateSpecified = true;
                        pReturnLeg.EffectiveDate.AdjustableDateSpecified = false;
                    }

                    _rowsAmount = GetRowInterestLegAmount(pFundingType, pDtBusiness);
                }
            }
            return _rowsAmount;
        }
        #endregion PrepareDailyInterestLegRow
        #region GetRowInterestLegAmount
        /// <summary>
        ///  Retourne les Evènements tels que EVENTTYPE = FDA|BWA et pour lesquels la date REC = {pDate}
        /// </summary>
        /// <param name="pDate"></param>
        /// <returns></returns>
        private IEnumerable<DataRow> GetRowInterestLegAmount(Cst.FundingType pFundingType, DateTime pDate)
        {
            IEnumerable<DataRow> _rowsCandidate = null;
            DataRow[] _rows = DsEvents.DtEvent.Select(StrFunc.AppendFormat(@"IDT = {0} and EVENTTYPE = '{1}' and INSTRUMENTNO = {2}", m_DsTrade.IdT,
            pFundingType == Cst.FundingType.Funding ? EventTypeFunc.FundingAmount : EventTypeFunc.BorrowingAmount,
            StrFunc.GetSuffixNumeric2(m_CurrentProduct.ProductBase.Id)));
            if (ArrFunc.IsFilled(_rows))
                _rowsCandidate = _rows.Where(_rowEvent => ExistEventClass(_rowEvent, EventClassFunc.Recognition, pDate));
            return _rowsCandidate;
        }
        #endregion GetRowInterestLegAmount
        #region CreateDailyRowInterestLegAmount
        /// <summary>
        /// Creation FDA|BWA du jour 
        /// </summary>
        /// <param name="pInterestLeg"></param>
        /// FI 20141215 [20570] Modify
        /// EG 20150317 [POC] : New Signature NewRowEvent
        /// EG 20150319 [POC] : New FDA et BWA
        /// FI 20170614 [23241] Modify
        // EG 20190613 [24683] Use slaveDbTransaction
        private void CreateDailyRowInterestLegAmount(Cst.FundingType pFundingType, Pair<IInterestLeg, IInterestCalculation> pInterestLeg)
        {
            DataRow rowInterestLeg = GetRowMainInterestLeg();
            if (null != rowInterestLeg)
            {
                IInterestLeg interestLeg = pInterestLeg.First;

                EFS_InterestLeg efs_InterestLeg = interestLeg.Efs_InterestLeg;
                efs_InterestLeg.CalcDailyPeriod(pInterestLeg);

                // FI 20141215 [20570] =>  foreach conservé mais normalement il ne peut y avoir qu'une seule période de 1J 
                foreach (EFS_InterestLegPaymentDate fda in efs_InterestLeg.paymentPeriods)
                {
                    // FI 20141215 [20570] use dtEnd
                    // si dtbusiness est un jeudi alors dtEnd est un jeudi (les intérêts porteront sur 1 jour)
                    // si dtbusiness est un vendredi alors dtEnd est un dimanche  (les intérêts porteront sur 3 jours (vendred, samedi et dimanche inclus)
                    // Particularité: 
                    // - DTENDADJ, DTENDUNADJ sont identiques et peuvent contenir une date non ajustée
                    // - DTENDADJ est inclus dans le calcul des intérêts, alors que classiquement les dates fins sont tjs exlues (see Classe:CalculationInterestLegInfo Méthod:SetInfoBase)

                    // FI 20170614 [23241] Prise en compte du business center de l'entité par défaut à la place du business center du marché
                    //DateTime dtEnd = m_EntityMarketInfo.dtMarketNext.AddDays(-1);
                    DateTime dtEnd = m_EntityMarketInfo.DtEntityNext.AddDays(-1);

                    // FI 20170614 [23241] Prise en compte du business center du custodian à la place du business center du marché
                    // FI 20170616 [23241] Changement de comportement 
                    // => Prise en compte du business Center de l'entité seulement
                    // => Abandon de la prise en compte du business center du custodian (voir ticket pour plus d'info)
                    /*
                    if (m_EntityMarketInfo.idA_CustodianSpecified)
                    {
                        string cs = CSTools.SetCacheOn(this.Process.Cs);
                        SQL_Actor sqlActor = new SQL_Actor(Cs, m_EntityMarketInfo.idA_Custodian);
                        if (false == sqlActor.LoadTable(new string[] { "IDBC" }))
                            throw new Exception(StrFunc.AppendFormat("Actor (Id:{0}) not found", m_EntityMarketInfo.idA_Custodian.ToString()));
                        string idBC = sqlActor.IdBC;
                        if (StrFunc.IsFilled(idBC))
                            dtEnd = Tools.CaclNextDay(Cs, this.productBase, m_EntityMarketInfo.dtEntity, DayTypeEnum.Business, new string[] { idBC }).AddDays(-1);
                    }
                    */

                    // FI 20141215 [20570] valorisation de DTENDUNADJ et DTENDADJ
                    // EG 20150317 [POC] : New Signature NewRowEvent
                    DataRow rowAmount = NewRowEvent(SlaveDbTransaction, rowInterestLeg, EventCodeFunc.LinkedProductPayment,
                        (pFundingType == Cst.FundingType.Funding ? EventTypeFunc.FundingAmount : EventTypeFunc.BorrowingAmount),
                        fda.AdjustedStartPeriod.DateValue, dtEnd, m_EventsValProcess.AppInstance);
                    DataRow rowAmount_REC = NewRowEventClass(Convert.ToInt32(rowAmount["IDE"]), EventClassFunc.Recognition, fda.AdjustedStartPeriod.DateValue, false);

                    // FI 20141215 [20570] add STL, ISPAYMENT=true
                    DataRow rowAmount_VAL = NewRowEventClass(Convert.ToInt32(rowAmount["IDE"]), EventClassFunc.ValueDate, fda.AdjustedStartPeriod.DateValue, false);

                    // FI 20141215 [20570] add STL, ISPAYMENT=true
                    DataRow rowAmount_STL = NewRowEventClass(Convert.ToInt32(rowAmount["IDE"]), EventClassFunc.Settlement, fda.AdjustedStartPeriod.DateValue, true);

                    DataRow rowAmount_Details = NewRowEventDet(rowAmount);
                    m_DsEvents.DtEvent.Rows.Add(rowAmount);
                    m_DsEvents.DtEventClass.Rows.Add(rowAmount_REC);
                    m_DsEvents.DtEventClass.Rows.Add(rowAmount_VAL);
                    m_DsEvents.DtEventClass.Rows.Add(rowAmount_STL);
                    m_DsEvents.DtEventDet.Rows.Add(rowAmount_Details);

                    if (pInterestLeg.Second.FloatingRateSpecified)
                    {
                        // Insertion du Reset
                        DataRow rowAsset = GetRowAsset(Convert.ToInt32(rowInterestLeg["IDE"])).FirstOrDefault();
                        DataRow rowAmount_Asset = NewRowEventAsset(rowAsset, Convert.ToInt32(rowAmount["IDE"]));
                        m_DsEvents.DtEventAsset.Rows.Add(rowAmount_Asset);


                        foreach (EFS_InterestLegResetDate _reset in fda.resetDates)
                        {
                            DataRow _rowReset = NewRowEvent(SlaveDbTransaction, rowAmount, EventCodeFunc.Reset, EventTypeFunc.FloatingRate, _reset.AdjustedStartPeriod.DateValue, dtEnd,
                                m_EventsValProcess.AppInstance);
                            _rowReset["IDA_PAY"] = Convert.DBNull;
                            _rowReset["IDB_PAY"] = Convert.DBNull;
                            _rowReset["IDA_REC"] = Convert.DBNull;
                            _rowReset["IDB_REC"] = Convert.DBNull;

                            DataRow _rowReset_REC = NewRowEventClass(Convert.ToInt32(_rowReset["IDE"]), EventClassFunc.GroupLevel, _reset.AdjustedStartPeriod.DateValue, false);
                            DataRow _rowReset_FXG = NewRowEventClass(Convert.ToInt32(_rowReset["IDE"]), EventClassFunc.Fixing, _reset.fixingDateAdjustment.AdjustedEventDate.DateValue, false);
                            DataRow _rowReset_Details = NewRowEventDet(_rowReset);
                            DataRow _rowReset_Asset = NewRowEventAsset(rowAsset, Convert.ToInt32(_rowReset["IDE"]));

                            m_DsEvents.DtEvent.Rows.Add(_rowReset);
                            m_DsEvents.DtEventClass.Rows.Add(_rowReset_REC);
                            m_DsEvents.DtEventClass.Rows.Add(_rowReset_FXG);
                            m_DsEvents.DtEventAsset.Rows.Add(_rowReset_Asset);
                            m_DsEvents.DtEventDet.Rows.Add(_rowReset_Details);
                        }
                    }
                }
            }
        }
        #endregion CreateDailyRowInterestLegAmount
        #region GetInterestLegRate
        /// <summary>
        /// Lecture du FundingRate|Borrowing en cours (dans référentiel). 
        /// </summary>
        /// <returns></returns>
        /// EG 20150309 [POC-BERKELEY] : New
        /// EG 20170510 [23153] Refactoring
        private IInterestCalculation GetInterestLegRate(Cst.FundingType pFundingType)
        {
            IInterestCalculation interest = m_ReturnSwapContainer.InterestLeg.InterestCalculation;
            FundingProcessing funding = TradeCaptureGen.GetFunding(pFundingType, m_CurrentFeeRequest);
            if (null != funding)
                interest = funding.FundingResponse.InterestCalculation;
            return interest;
        }
        #endregion GetInterestLegRate




        #endregion Methods
    }
    #endregion EventsValProcessRTS
}
