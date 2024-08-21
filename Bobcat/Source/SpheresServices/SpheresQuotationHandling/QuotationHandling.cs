#region Using Directives
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
//
using EFS.ACommon;
using EFS.ApplicationBlocks.Data;
using EFS.ApplicationBlocks.Data.Extension;
using EFS.Common;
using EFS.Common.Log;
using EFS.Common.MQueue;
using EFS.LoggerClient;
using EFS.LoggerClient.LoggerService;
using EFS.SpheresService;
using EFS.TradeLink;
//
using EfsML.Business;
using EfsML.Enum;
using EfsML.Enum.Tools;
//
using FixML.Enum;
//
using FpML.Enum;
using FpML.Interface;
#endregion Using Directives

namespace EFS.Process
{
    /// <summary>
    /// 
    /// </summary>
    public class QuotationHandlingProcess : ProcessBase
    {
        #region Members
        /// <summary>
        /// Jeu de résultat
        /// </summary>
        private DataSet _ds;
        /// <summary>
        /// Représente le message qui a sollicité le service QuotationHandling
        /// </summary>
        private readonly QuotationHandlingMQueue _quotationHandlingMQueue;
        #endregion Members
        #region Accessors
        /// <summary>
        ///  Obtient la cotation en cas de modification de prix
        /// </summary>
        protected Quote Quote
        {
            get
            {
                Quote ret = null;
                if (_quotationHandlingMQueue.quoteSpecified)
                    ret = (Quote)_quotationHandlingMQueue.quote;
                return ret;
            }
        }
        /// <summary>
        /// Obtient la maturité en cas de modification de maturité
        /// </summary>
        protected Maturity Maturity
        {
            get
            {
                Maturity ret = null;
                if (_quotationHandlingMQueue.maturitySpecified)
                    ret = (Maturity)_quotationHandlingMQueue.maturity;
                return ret;
            }
        }
        /// <summary>
        /// 
        /// </summary>
        protected override string DataIdent
        {
            get
            {
                string ret = string.Empty;

                if (null != Quote)
                    ret = Quote.QuoteTable;
                else if (null != Maturity)
                    ret = Cst.OTCml_TBL.MATURITY.ToString();

                return ret;
            }
        }
        #endregion
        #region Constructor
        public QuotationHandlingProcess(MQueueBase pMQueue, AppInstanceService pAppInstance)
            : base(pMQueue, pAppInstance)
        {
            _quotationHandlingMQueue = (QuotationHandlingMQueue)pMQueue;
        }
        #endregion Constructor

        #region Methods
        /// <summary>
        /// Exécution du process
        /// </summary>
        /// <returns></returns>
        /// EG 20100118 Change PostMQueue (ETDQuotation (CLOSINGGEN) and Others (EVENTSVALGEN))
        /// EG 20220825 [26083][WI412] Réécriture du traitement de mise à jour des dates d'échéance sur les tables TRADE, EVENT et tables liées 
        protected override Cst.ErrLevel ProcessExecuteSpecific()
        {
            Cst.ErrLevel codeReturn = Cst.ErrLevel.NOTHINGTODO;

            if (_quotationHandlingMQueue.quoteSpecified)
            {
                codeReturn = ProcessExecuteQuotation();
            }
            else if (_quotationHandlingMQueue.maturitySpecified)
            {
                codeReturn = ProcessExecuteMaturity2();
            }

            return codeReturn;
        }

        /// <summary>
        /// Entrée principale de quoteHandling si existence d'une cotation dans le message
        /// </summary>
        /// <returns></returns>
        private Cst.ErrLevel ProcessExecuteQuotation()
        {
            Cst.ErrLevel ret = ProcessExecuteQuotationTrade();

            if ((ret == Cst.ErrLevel.SUCCESS) || (ret == Cst.ErrLevel.NOTHINGTODO))
                ret = ProcessExecuteQuotationCollateral();

            // FI 20190704 [XXXXX] NOTHINGTODO est une valeur possible pour le retour de cette méthode
            // => Le tracker est alors en bleu signifiant ainsi à l'utilisateur que sa modification de prix est sans impact
            // RD 20200504 [25323] Le tracker en success si aucune modification
            // RD 20200525 [25323] Rollback de la modification du 20200504
            //if (ret == Cst.ErrLevel.NOTHINGTODO)
            //    ret = Cst.ErrLevel.SUCCESS;

            return ret;
        }

   

        /// <summary>
        /// Chargement d'un table de travail contenant tous les EVTS concernés par le changement
        /// de date de maturité.
        /// - Recherche des EVTS qui ont un IDASSET (ORIGIN = S)
        /// - Recherche des EVTS enfants (ORIGIN = C), avec critères de restriction sur la base des EVTS retournés par le select précédent (ORIGIN = S)
        /// - Recherche des EVTS parents (ORIGIN = P), sur la base des EVTS retournés par le select précédent (ORIGIN = S)
        /// 
        /// Insert dans la table de travail via ce Select
        /// </summary>
        /// <param name="pDbTransaction"></param>
        /// <param name="pTableWork"></param>
        /// <returns></returns>
        /// EG 20220825 [26083][WI412] Réécriture du traitement de mise à jour des dates d'échéance sur les tables TRADE, EVENT et tables liées 
        /// EG 20220519 [WI638] Refactoring
        private int LoadTableWithEventsCandidatesByMaturity(string pTableWork)
        {
            int idAsset = 0;
            string sqlQuery = string.Empty;

            DbSvrType serverType = DataHelper.GetDbSvrType(Cs);
            if (DbSvrType.dbSQL == serverType)
            {
                sqlQuery = String.Format(@"with EventTree (IDT, IDE, IDE_EVENT, EVENTCODE, EVENTTYPE, IDASSET, ORIGIN) as 
                ( 
                    /* Recherche des EVTS qui ont un IDASSET */
                    select e.IDT, e.IDE, e.IDE_EVENT, e.EVENTCODE, e.EVENTTYPE, ea.IDASSET, 'S' as ORIGIN
                    from dbo.MATURITY ma
                    inner join dbo.DERIVATIVEATTRIB da on (da.IDMATURITY=ma.IDMATURITY)
                    inner join dbo.DERIVATIVECONTRACT dc on (dc.IDDC=da.IDDC) 
                    inner join dbo.ASSET_ETD a on (a.IDDERIVATIVEATTRIB=da.IDDERIVATIVEATTRIB)
                    inner join dbo.EVENTASSET ea on (ea.IDASSET=a.IDASSET) and (ea.ASSETTYPE = @ASSETTYPE)
                    inner join dbo.EVENT e on (e.IDE=ea.IDE) 
                    where (ma.IDMATURITY = @IDMATURITY)
                )
                insert into dbo.{0}
                select distinct et.IDT, t.IDENTIFIER, et.IDE, et.IDE_EVENT, et.EVENTCODE,et.EVENTTYPE, et.IDASSET, et.ORIGIN 
                from EventTree et
                inner join dbo.TRADE t on (t.IDT=et.IDT)
                union all
                select distinct e.IDT, tr.IDENTIFIER, e.IDE, e.IDE_EVENT, e.EVENTCODE, e.EVENTTYPE, et.IDASSET, 'C'
                from EventTree et
                inner join dbo.TRADE tr on (tr.IDT = et.IDT)
                inner join dbo.EVENT e on (e.IDE_EVENT=et.IDE)
                where (et.ORIGIN = 'S') and	(
                    ((e.EVENTCODE='LPC') and (e.EVENTTYPE='AMT')) or
                    ((e.EVENTCODE='LPP') and (e.EVENTTYPE in ('PRM','HPR'))) or
                    (e.EVENTCODE in ('EXD','ASD') and e.EVENTTYPE in ('EUR','AME')))
                union all
                select distinct e.IDT, tr.IDENTIFIER, e.IDE, e.IDE_EVENT, e.EVENTCODE, e.EVENTTYPE, et.IDASSET, 'P'
                from EventTree et
                inner join dbo.TRADE tr on (tr.IDT = et.IDT)
                inner join dbo.EVENT e on (e.IDE = et.IDE_EVENT)
                where et.ORIGIN = 'S';", pTableWork);
            }
            else if (DbSvrType.dbORA == serverType)
            {
                sqlQuery = String.Format(@"insert into dbo.{0} with EventTree (IDT, IDE, IDE_EVENT, EVENTCODE, EVENTTYPE, IDASSET, ORIGIN) as 
                ( 
                    /* Recherche des EVTS qui ont un IDASSET */
                    select e.IDT, e.IDE, e.IDE_EVENT, e.EVENTCODE, e.EVENTTYPE, ea.IDASSET, 'S' as ORIGIN
                    from dbo.MATURITY ma
                    inner join dbo.DERIVATIVEATTRIB da on (da.IDMATURITY=ma.IDMATURITY)
                    inner join dbo.DERIVATIVECONTRACT dc on (dc.IDDC=da.IDDC)
                    inner join dbo.ASSET_ETD a on (a.IDDERIVATIVEATTRIB=da.IDDERIVATIVEATTRIB)
                    inner join dbo.EVENTASSET ea on (ea.IDASSET=a.IDASSET) and (ea.ASSETTYPE = @ASSETTYPE)
                    inner join dbo.EVENT e on (e.IDE=ea.IDE)
                    where (ma.IDMATURITY = @IDMATURITY)


                    union all 

                    /* Recherche des EVTS enfants (avec critères de restriction) des EVTS retournés par le select précédent (ORIGIN = S) */
                    select e.IDT, e.IDE, e.IDE_EVENT, e.EVENTCODE, e.EVENTTYPE, et.IDASSET, 'C' as ORIGIN
                    from EventTree et
                    inner join dbo.EVENT e on (e.IDE_EVENT = et.IDE)
                    where (et.ORIGIN = 'S') and	
                    (
                        ((e.EVENTCODE='LPC') and (e.EVENTTYPE='AMT')) or
                        ((e.EVENTCODE='LPP') and (e.EVENTTYPE in ('PRM','HPR'))) or
                        (e.EVENTCODE in ('EXD','ASD') and e.EVENTTYPE in ('EUR','AME'))
                    )
                )
                select distinct et.IDT, t.IDENTIFIER, et.IDE, et.IDE_EVENT, et.EVENTCODE,et.EVENTTYPE, et.IDASSET, et.ORIGIN
                from EventTree et
                inner join dbo.TRADE t on (t.IDT=et.IDT)

                union all

                select distinct et.IDT, t.IDENTIFIER, e.IDE, e.IDE_EVENT, e.EVENTCODE, e.EVENTTYPE, et.IDASSET, 'P' as ORIGIN
                from EventTree et
                inner join dbo.EVENT e on (e.IDE = et.IDE_EVENT)
                inner join dbo.TRADE t on (t.IDT=et.IDT)
                where et.ORIGIN = 'S';", pTableWork);
            }

            DataParameters dp = new DataParameters();
            dp.Add(new DataParameter(Cs, "IDMATURITY", DbType.Int32), Maturity.idMaturity);
            dp.Add(new DataParameter(Cs, "ASSETTYPE", DbType.AnsiString, SQLCst.UT_ENUM_MANDATORY_LEN), QuoteEnum.ETD.ToString());
            QueryParameters qryParameters = new QueryParameters(Cs, sqlQuery, dp);
            DataHelper.ExecuteNonQuery(Cs, CommandType.Text, qryParameters.Query, qryParameters.Parameters.GetArrayDbParameter());

            // On récupère le premier IdAsset de la table de travail pour ajuster la nouvelle date de maturité sur un jour ouvré
            sqlQuery = $"select tw.IDT, tw.IDE, tw.IDASSET from {pTableWork} tw where tw.ORIGIN = 'S' order by tw.IDASSET offset 0 rows fetch next 1 rows only";
            using (IDataReader dr = DataHelper.ExecuteReader(Cs, CommandType.Text, sqlQuery))
            {
                if (dr.Read())
                    idAsset = Convert.ToInt32(dr["IDASSET"]);
            }

            if (DbSvrType.dbORA == serverType)
            {
                Common.AppInstance.TraceManager.TraceVerbose(null, string.Format("update statistic on {0}", pTableWork));
                DataHelper.UpdateStatTable(Cs, pTableWork);
            }

            return idAsset;
        }

        /// <summary>
        /// Retourne la table de travail utilisée pour la mise à jour
        /// des dates sur les EVTs (et autres tables) concernés par la date de maturité
        /// - Création si inexistante sur la base du modèle EVENTBYMATURITY_MODEL ou truncate/Delete
        /// - Création des index
        /// </summary>
        /// <returns></returns>
        /// EG 20220825 [26083][WI412] Réécriture du traitement de mise à jour des dates d'échéance sur les tables TRADE, EVENT et tables liées 
        private string GetWorkTableName()
        {
            string tableWork = String.Format("EVENTBYMATURITY_{0}_W", Session.BuildTableId());
            if (tableWork.Length > 32)
                throw new Exception(StrFunc.AppendFormat("Table Name :{0} is too long", tableWork));

            DbSvrType serverType = DataHelper.GetDbSvrType(Cs);
            bool isExist = DataHelper.IsExistTable(Cs, tableWork);
            if (isExist)
            {
                string command;
                if (DbSvrType.dbSQL == serverType)
                    command = String.Format(@"truncate table dbo.{0};", tableWork);
                else if (DbSvrType.dbORA == serverType)
                    command = String.Format(@"delete from dbo.{0};", tableWork);
                else
                    throw new NotImplementedException("RDBMS not implemented");
                QueryParameters qryParameters = new QueryParameters(Cs, command, null);
                DataHelper.ExecuteNonQuery(Cs, CommandType.Text, qryParameters.Query);
            }
            else
            {
                DataHelper.CreateTableAsSelect(Cs, "EVENTBYMATURITY_MODEL", tableWork, out _);

                string command;
                if (DbSvrType.dbSQL == serverType)
                    command = String.Format("create clustered index IX_{0} on dbo.{0} ({1})", tableWork, "IDE, ORIGIN, EVENTCODE, EVENTTYPE");
                else if (DbSvrType.dbORA == serverType)
                    command = String.Format("create index IX_{0} on dbo.{0} ({1})", tableWork, "IDE, ORIGIN, EVENTCODE, EVENTTYPE");
                else
                    throw new NotImplementedException("RDBMS not implemented");

                Common.AppInstance.TraceManager.TraceVerbose(null, string.Format("Name:{0} - SQL:{1}", "create index IX", command));
                DataHelper.ExecuteNonQuery(Cs, CommandType.Text, command);

                command = String.Format("create index IX_{0}_1 on dbo.{0} ({1})", tableWork, "IDE_EVENT, ORIGIN, EVENTCODE, EVENTTYPE");
                Common.AppInstance.TraceManager.TraceVerbose(null, string.Format("Name:{0} - SQL:{1}", "create index IX", command));
                DataHelper.ExecuteNonQuery(Cs, CommandType.Text, command);

                command = String.Format("create index IX_{0}_2 on dbo.{0} ({1})", tableWork, "IDT, ORIGIN, EVENTCODE, EVENTTYPE");
                Common.AppInstance.TraceManager.TraceVerbose(null, string.Format("Name:{0} - SQL:{1}", "create index IX", command));
                DataHelper.ExecuteNonQuery(Cs, CommandType.Text, command);
            }
            return tableWork;
        }

        /// <summary>
        /// Procédure principale du traitement de mise à jour des données suite à modification
        /// d'une date d'échéance d'un contrat
        /// </summary>
        /// <returns></returns>
        /// EG 20220825 [26083][WI412] Réécriture du traitement de mise à jour des dates d'échéance sur les tables TRADE, EVENT et tables liées 
        private Cst.ErrLevel ProcessExecuteMaturity2()
        {
            Cst.ErrLevel ret = Cst.ErrLevel.NOTHINGTODO;
            IDbTransaction dbTransaction = null;
            try
            {
                if (null == Maturity)
                    throw new InvalidProgramException("Maturuty is null");

                DataRowState action = (DataRowState)Enum.Parse(typeof(DataRowState), Maturity.action);

                DateTime maturityDate = DateTime.MinValue;
                DateTime maturityDateSys = DateTime.MinValue;
                DateTime deliveryDate = DateTime.MinValue;

                bool isNoPeriodicDelivery = true;
                SQL_MaturityRuleRead sql_MaturityRule = new SQL_MaturityRuleRead(Cs, Maturity.idMaturityRule);
                if (sql_MaturityRule.IsLoaded)
                {
                    isNoPeriodicDelivery = sql_MaturityRule.IsNoPeriodicDelivery;
                }
                else
                {
                    throw new NotSupportedException(StrFunc.AppendFormat("MaturityRule (ID:{0}) not found", Maturity.idMaturityRule.ToString()));
                }

                SQL_Maturity sql_Maturity = new SQL_Maturity(Cs, Maturity.idMaturity);
                if (sql_Maturity.IsLoaded)
                {
                    maturityDate = sql_Maturity.MaturityDate;
                    maturityDateSys = sql_Maturity.MaturityDateSys;
                    if (isNoPeriodicDelivery)
                        deliveryDate = sql_Maturity.DelivryDate;
                }
                else
                {
                    throw new NotSupportedException(StrFunc.AppendFormat("Maturity (ID:{0}) not found", Maturity.idMaturity.ToString()));
                }
                bool isNewDateMaxValue = (DtFunc.IsDateTimeEmpty(maturityDate) || (action == DataRowState.Deleted));


                string tableWork = GetWorkTableName();

                int idAsset = LoadTableWithEventsCandidatesByMaturity(tableWork);

                // PM 20130822 [18404] Décalage de 0 jour business preceding pour que la date d'échéance soit une Business Date
                if (!isNewDateMaxValue && (idAsset > 0))
                {
                    // Pas glop : on prend le Business Center du marché du premier asset
                    SQL_AssetETD asset = new SQL_AssetETD(Cs, idAsset);
                    IProduct product = new EfsML.v30.Fix.ExchangeTradedDerivative();
                    IBusinessDayAdjustments maturityBDA = product.ProductBase.CreateBusinessDayAdjustments(BusinessDayConventionEnum.PRECEDING, asset.Market_IDBC);
                    IOffset maturityOffset = product.ProductBase.CreateOffset(PeriodEnum.D, 0, DayTypeEnum.ExchangeBusiness);
                    maturityDate = Tools.ApplyOffset(Cs, maturityDate, maturityOffset, maturityBDA, null);
                }

                if (0 < idAsset)
                {
                    dbTransaction = DataHelper.BeginTran(Cs);

                    // Mise à jour des événements (ORIGIN = S|C)
                    UpdateEventWithMaturity_SourceAndChild(dbTransaction, tableWork, maturityDate);
                    // Mise à jour des événements PRD/DAT (ORIGIN = P)
                    UpdateEventWithMaturity_Product(dbTransaction, tableWork);
                    // Mise à jour des événements TRD/DAT (Parent de ORIGIN = P)
                    // pour prendre en considération les PRD/DAT updatés précédemment
                    UpdateEventWithMaturity_Trade(dbTransaction, tableWork);
                    // Mise à jour EVENTCLASS
                    UpdateEventClassWithMaturity(dbTransaction, tableWork, maturityDate);
                    // Mise à jour EVENTASSET
                    UpdateEventAssetWithMaturity(dbTransaction, tableWork, maturityDate, maturityDateSys, deliveryDate);
                    // Mise à jour TRADE (DTOUTADJ/DTOUTUNADJ)
                    UpdateTradeWithMaturity(dbTransaction, tableWork);
                    // Mise à jour TRADEINSTRUMENT (DTOUTADJ/DTOUTUNADJ)
                    UpdateTradeInstrumentWithMaturity(dbTransaction, tableWork);
                    // Mise à jour TRADESTREAM (DTOUTADJ/DTOUTUNADJ)
                    UpdateTradeStreamWithMaturity(dbTransaction, tableWork);
                    // Mise à jour TRADE (DTOUT)
                    UpdateTradeDTOUTWithMaturity(dbTransaction, sql_Maturity.Id, maturityDateSys);
                    // Mise à jour POSACTION (DTOUT)
                    UpdatePosactionWithMaturity(dbTransaction, sql_Maturity.Id, maturityDateSys);

                    ret = Cst.ErrLevel.SUCCESS;

                    DataHelper.CommitTran(dbTransaction);
                }
                else
                {
                    ret = Cst.ErrLevel.NOTHINGTODO;

                    Logger.Log(new LoggerData(LogLevelEnum.Info, new SysMsgCode(SysCodeEnum.LOG, 406), 0,
                        new LogParam(CurrentId, default, default, Cst.LoggerParameterLink.IDDATA)));
                }

                if (ret == Cst.ErrLevel.SUCCESS)
                {
                    List<MapDataReaderRow> lstMapRow = null;
                    bool isOneTrade = false;
                    string tradeModifiedMsg = string.Empty;

                    // Lectures des 10 premiers trades mis à jour pour remplissage du log
                    string sqlSelect = String.Format(@"select IDENTIFIER, IDT, count(IDE) as IDE
                    from dbo.{0} tw group by IDENTIFIER, IDT
                    order by IDT offset 0 rows fetch next 10 rows only", tableWork);
                    using (IDataReader dr = DataHelper.ExecuteReader(Cs, CommandType.Text, sqlSelect))
                    {
                        lstMapRow = DataReaderExtension.DataReaderMapToList(dr);
                    }

                    if (0 < lstMapRow.Count)
                    {
                        isOneTrade = (1 == lstMapRow.Count);
                        lstMapRow.ForEach(row =>
                        {
                            tradeModifiedMsg += LogTools.IdentifierAndId(row["IDENTIFIER"].Value.ToString(), Convert.ToInt32(row["IDT"].Value)) + (isOneTrade ? string.Empty : ", ") + Cst.CrLf;
                        });
                    }

                    if (128 < tradeModifiedMsg.Length)
                        tradeModifiedMsg = tradeModifiedMsg.Substring(1, 100) + " ...";
                    
                    Logger.Log(new LoggerData(LogLevelEnum.Info, new SysMsgCode(SysCodeEnum.LOG, 405), 0,
                        new LogParam(CurrentId, default, default, Cst.LoggerParameterLink.IDDATA),
                        new LogParam(isNewDateMaxValue ? DateTime.MaxValue.Date.ToLongDateString() : Maturity.maturityDate.ToLongDateString()),
                        new LogParam(LogTools.IdentifierAndId(Maturity.maturityMonthYear, Maturity.idMaturity)),
                        new LogParam(LogTools.IdentifierAndId(Maturity.idMaturityRule_identifier, Maturity.idMaturityRule)),
                        new LogParam(tradeModifiedMsg)));
                }

            }
            catch (Exception ex)
            {
                ret = Cst.ErrLevel.FAILURE;
                DataHelper.RollbackTran(dbTransaction);
                throw new SpheresException2(MethodInfo.GetCurrentMethod().Name, "SYS-00401", ex);
            }
            return ret;
        }

        /// <summary>
        /// Mise à jour des dates FIN sur les événements après modification d'une date d'échéance d'un contrat.
        /// </summary>
        /// <param name="pDbTransaction">Transaction en cours</param>
        /// <param name="pTableWork">Table de travail des principaux événements candidats à mise à jour</param>
        /// EG 20220825 [26083][WI412] Réécriture du traitement de mise à jour des dates d'échéance sur les tables TRADE, EVENT et tables liées 
        private void UpdateEventWithMaturity_SourceAndChild(IDbTransaction pDbTransaction, string pTableWork, DateTime pMaturityDate)
        {
            string sqlUpdate = string.Empty;
            string condition = "case when (tw.EVENTCODE in ('EXD','ASD')) and (tw.EVENTTYPE = 'EUR') then @MATURITYDATE";
            if (DataHelper.IsDbSqlServer(Cs))
            {
                sqlUpdate = @"update ev set ev.DTENDUNADJ= @MATURITYDATE, ev.DTENDADJ= @MATURITYDATE, 
                ev.DTSTARTUNADJ = {1} else ev.DTSTARTUNADJ end, ev.DTSTARTADJ= {1} else ev.DTSTARTADJ end
                from dbo.{0} tw
                inner join dbo.EVENT ev on (ev.IDE = tw.IDE)
                where tw.ORIGIN in ('C','S')";
            }
            else if (DataHelper.IsDbOracle(Cs))
            {
                sqlUpdate = @"update dbo.EVENT set (DTENDUNADJ, DTENDADJ, DTSTARTUNADJ, DTSTARTADJ) =
                (select @MATURITYDATE, @MATURITYDATE, {1} else EVENT.DTSTARTUNADJ end, {1} else EVENT.DTSTARTADJ end
                from dbo.{0} tw
                where (tw.IDE = EVENT.IDE) and (tw.ORIGIN in ('C','S')))
                where exists ( select 1 from dbo.{0} tw where (tw.IDE = EVENT.IDE) and (tw.ORIGIN in ('C','S')));";
            }

            sqlUpdate = String.Format(sqlUpdate, pTableWork, condition);

            DataParameters dp = new DataParameters();
            dp.Add(DataParameter.GetParameter(Cs, DataParameter.ParameterEnum.MATURITYDATE), pMaturityDate);
            QueryParameters qryParameters = new QueryParameters(Cs, sqlUpdate, dp);
            DataHelper.ExecuteNonQuery(pDbTransaction, CommandType.Text, qryParameters.Query, qryParameters.Parameters.GetArrayDbParameter());
        }
        /// <summary>
        /// Mise à jour des dates FIN sur les événements PRD/DAT 
        /// après modification d'une date d'échéance d'un ETD.
        /// </summary>
        /// <param name="pDbTransaction">Transaction en cours</param>
        /// <param name="pTableWork">Table de travail des principaux événements candidats à mise à jour</param>
        /// EG 20220825 [26083][WI412] Réécriture du traitement de mise à jour des dates d'échéance sur les tables TRADE, EVENT et tables liées 
        private void UpdateEventWithMaturity_Product(IDbTransaction pDbTransaction, string pTableWork)
        {
            string sqlUpdate = string.Empty;
            if (DataHelper.IsDbSqlServer(Cs))
            {
                sqlUpdate = @"with UPDPRD as (
                select tw.IDE, MAX(evch.DTENDUNADJ) as MAXDTENDUNADJ, MAX(evch.DTENDADJ) as MAXDTENDADJ
                from dbo.{0} tw
                inner join dbo.EVENT ev on (ev.IDE = tw.IDE)
                inner join dbo.EVENT evch on (evch.IDE_EVENT = tw.IDE)
                where (tw.ORIGIN = 'P') and (tw.EVENTCODE = @EVENTCODE) and (tw.EVENTTYPE = @EVENTTYPE)
                group by tw.IDE
                )
                update ev set ev.DTENDUNADJ= upd.MAXDTENDUNADJ, ev.DTENDADJ= upd.MAXDTENDADJ
                from dbo.EVENT ev
                inner join UPDPRD upd on (upd.IDE = ev.IDE);";
            }
            else if (DataHelper.IsDbOracle(Cs))
            {
                sqlUpdate = @"update dbo.EVENT set (DTENDUNADJ, DTENDADJ) =  
                (
                    select MAX(evch.DTENDUNADJ) as MAXDTENDUNADJ, MAX(evch.DTENDADJ) as MAXDTENDADJ
                    from dbo.{0} tw
                    inner join dbo.EVENT evch on (evch.IDE_EVENT = tw.IDE)
                    where (tw.IDE = EVENT.IDE) and (tw.ORIGIN = 'P') and (tw.EVENTCODE = @EVENTCODE) and (tw.EVENTTYPE = @EVENTTYPE)
                    group by tw.IDE
                )
                where exists ( select 1 
                from dbo.{0} tw
                inner join dbo.EVENT evch on (evch.IDE_EVENT = tw.IDE)
                where (tw.IDE = EVENT.IDE) and (tw.ORIGIN = 'P') and (tw.EVENTCODE = @EVENTCODE) and (tw.EVENTTYPE = @EVENTTYPE));";
            }

            sqlUpdate = String.Format(sqlUpdate, pTableWork);

            DataParameters dp = new DataParameters();
            dp.Add(DataParameter.GetParameter(Cs, DataParameter.ParameterEnum.EVENTCODE), EventCodeFunc.Product);
            dp.Add(DataParameter.GetParameter(Cs, DataParameter.ParameterEnum.EVENTTYPE), EventTypeFunc.Date);
            QueryParameters qryParameters = new QueryParameters(Cs, sqlUpdate, dp);
            DataHelper.ExecuteNonQuery(pDbTransaction, CommandType.Text, qryParameters.Query, qryParameters.Parameters.GetArrayDbParameter());
        }
        /// <summary>
        /// Mise à jour des dates FIN sur les événements TRD/DAT 
        /// après modification d'une date d'échéance d'un ETD.
        /// </summary>
        /// <param name="pDbTransaction">Transaction en cours</param>
        /// <param name="pTableWork">Table de travail des principaux événements candidats à mise à jour</param>
        /// EG 20220825 [26083][WI412] Réécriture du traitement de mise à jour des dates d'échéance sur les tables TRADE, EVENT et tables liées 
        private void UpdateEventWithMaturity_Trade(IDbTransaction pDbTransaction, string pTableWork)
        {
            string sqlUpdate = string.Empty;
            if (DataHelper.IsDbSqlServer(Cs))
            {
                sqlUpdate = @"with UPDTRD as (
                select tw.IDE_EVENT, MAX(ev.DTENDUNADJ) as MAXDTENDUNADJ, MAX(ev.DTENDADJ) as MAXDTENDADJ
                from dbo.{0} tw
                inner join dbo.EVENT ev on (ev.IDE = tw.IDE)
                where (tw.ORIGIN = 'P') and (tw.EVENTCODE = @EVENTCODE) and (tw.EVENTTYPE = @EVENTTYPE)
                group by tw.IDE_EVENT
                )
                update ev set ev.DTENDUNADJ= upd.MAXDTENDUNADJ, ev.DTENDADJ= upd.MAXDTENDADJ
                from dbo.EVENT ev
                inner join UPDTRD upd on (upd.IDE_EVENT = ev.IDE);";
            }
            else if (DataHelper.IsDbOracle(Cs))
            {
                sqlUpdate = @"update dbo.EVENT set (DTENDUNADJ, DTENDADJ) =  
                (
                    select MAX(ev.DTENDUNADJ) as MAXDTENDUNADJ, MAX(ev.DTENDADJ) as MAXDTENDADJ
                    from dbo.{0} tw
                    inner join dbo.EVENT ev on (ev.IDE = tw.IDE)
                    where (tw.IDE_EVENT = EVENT.IDE) and (tw.ORIGIN = 'P') and (tw.EVENTCODE = @EVENTCODE) and (tw.EVENTTYPE = @EVENTTYPE)
                    group by tw.IDE_EVENT
                )
                where exists ( select 1 
                from dbo.{0} tw
                inner join dbo.EVENT ev on (ev.IDE = tw.IDE)
                where (tw.IDE_EVENT = EVENT.IDE) and (tw.ORIGIN = 'P') and (tw.EVENTCODE = @EVENTCODE) and (tw.EVENTTYPE = @EVENTTYPE));";
            }

            sqlUpdate = String.Format(sqlUpdate, pTableWork);

            DataParameters dp = new DataParameters();
            dp.Add(DataParameter.GetParameter(Cs, DataParameter.ParameterEnum.EVENTCODE), EventCodeFunc.Product);
            dp.Add(DataParameter.GetParameter(Cs, DataParameter.ParameterEnum.EVENTTYPE), EventTypeFunc.Date);
            QueryParameters qryParameters = new QueryParameters(Cs, sqlUpdate, dp);
            DataHelper.ExecuteNonQuery(pDbTransaction, CommandType.Text, qryParameters.Query, qryParameters.Parameters.GetArrayDbParameter());

        }
        /// <summary>
        /// Mise à jour de EVENTCLASS (DTEVENT)
        /// après modification d'une date d'échéance d'un ETD.
        /// </summary>
        /// <param name="pDbTransaction">Transaction en cours</param>
        /// <param name="pTableWork">Table de travail des principaux événements candidats à mise à jour</param>
        /// EG 20220825 [26083][WI412] Réécriture du traitement de mise à jour des dates d'échéance sur les tables TRADE, EVENT et tables liées 
        private void UpdateEventClassWithMaturity(IDbTransaction pDbTransaction, string pTableWork, DateTime pMaturityDate)
        {
            DateTime dtSysBusiness = OTCmlHelper.GetDateBusiness(Cs).Date;
            DateTime dtEventForced = pMaturityDate;
            if (0 >= DateTime.Compare(pMaturityDate, dtSysBusiness))
                dtEventForced = dtSysBusiness;

            string sqlUpdate = string.Empty;
            if (DataHelper.IsDbSqlServer(Cs))
            {
                sqlUpdate = @"update ec set ec.DTEVENT= @DTEVENT, ec.DTEVENTFORCED= @DTEVENTFORCED 
                from dbo.{0} tw
                inner join dbo.EVENTCLASS ec on (ec.IDE = tw.IDE)
                where (tw.ORIGIN in ('C','S')) and (ec.EVENTCLASS in ('GRP','PHY','CSH'))";
            }
            else if (DataHelper.IsDbOracle(Cs))
            {
                sqlUpdate = @"update dbo.EVENTCLASS set (DTEVENT, DTEVENTFORCED) =
                (select @DTEVENT, @DTEVENTFORCED
                from dbo.{0} tw
                where (tw.IDE = EVENTCLASS.IDE) and (tw.ORIGIN in ('C','S')) and (EVENTCLASS.EVENTCLASS in ('GRP','PHY','CSH')))
                where exists ( select 1 from dbo.{0} tw where (tw.IDE = EVENTCLASS.IDE)
                and (tw.ORIGIN in ('C','S')) and (EVENTCLASS.EVENTCLASS in ('GRP','PHY','CSH')));";
            }

            sqlUpdate = String.Format(sqlUpdate, pTableWork);

            DataParameters dp = new DataParameters();
            dp.Add(DataParameter.GetParameter(Cs, DataParameter.ParameterEnum.DTEVENT), pMaturityDate);
            dp.Add(DataParameter.GetParameter(Cs, DataParameter.ParameterEnum.DTEVENTFORCED), dtEventForced);
            QueryParameters qryParameters = new QueryParameters(Cs, sqlUpdate, dp);
            DataHelper.ExecuteNonQuery(pDbTransaction, CommandType.Text, qryParameters.Query, qryParameters.Parameters.GetArrayDbParameter());
        }
        /// <summary>
        /// Mise à jour de EVENTASSET (MATURITYDATE/DELIVERYDATE)
        /// </summary>
        /// <param name="pDbTransaction">Transaction en cours</param>
        /// <param name="pTableWork">Table de travail des principaux événements candidats à mise à jour</param>
        /// <param name="pMaturityDate">Nouvelle date de maturité</param>
        /// <param name="pMaturityDateSys">Nouvelle date de maturité système</param>
        /// <param name="pDeliveryDate">Nouvelle date de livraison</param>
        /// EG 20220825 [26083][WI412] Réécriture du traitement de mise à jour des dates d'échéance sur les tables TRADE, EVENT et tables liées 
        private void UpdateEventAssetWithMaturity(IDbTransaction pDbTransaction, string pTableWork, DateTime pMaturityDate, DateTime pMaturityDateSys, DateTime pDeliveryDate)
        {
            string sqlUpdate = string.Empty;

            if (DataHelper.IsDbSqlServer(Cs))
            {
                sqlUpdate = @"update ea set ea.MATURITYDATE=@MATURITYDATE, ea.MATURITYDATESYS= @MATURITYDATESYS, ea.DELIVERYDATE= @DELIVERYDATE 
                from dbo.{0} tw
                inner join dbo.EVENTASSET ea on (ea.IDE = tw.IDE) and (tw.IDASSET = ea.IDASSET)
                where (tw.ORIGIN = 'S')";
            }
            else if (DataHelper.IsDbOracle(Cs))
            {
                sqlUpdate = @"update dbo.EVENTASSET ea set (MATURITYDATE, MATURITYDATESYS, DELIVERYDATE) =
                (select @MATURITYDATE, @MATURITYDATESYS, @DELIVERYDATE
                from dbo.{0} tw
                where (tw.IDE = ea.IDE) and (tw.IDASSET = ea.IDASSET) and (tw.ORIGIN = 'S'))
                where exists ( select 1 from dbo.{0} tw where (tw.IDE = ea.IDE) and (tw.IDASSET = ea.IDASSET) and (tw.ORIGIN = 'S'));";
            }

            sqlUpdate = String.Format(sqlUpdate, pTableWork);

            DataParameters dp = new DataParameters();
            dp.Add(new DataParameter(Cs, "MATURITYDATE", DbType.DateTime), pMaturityDate);
            dp.Add(new DataParameter(Cs, "MATURITYDATESYS", DbType.DateTime), DtFunc.IsDateTimeFilled(pMaturityDateSys)?pMaturityDateSys:Convert.DBNull);
            dp.Add(new DataParameter(Cs, "DELIVERYDATE", DbType.Date), DtFunc.IsDateTimeFilled(pDeliveryDate)? pDeliveryDate:Convert.DBNull);

            QueryParameters qryParameters = new QueryParameters(Cs, sqlUpdate, dp);
            DataHelper.ExecuteNonQuery(pDbTransaction, CommandType.Text, qryParameters.Query, qryParameters.Parameters.GetArrayDbParameter());
        }

        /// <summary>
        /// Mise à jour de la table TRADE (DTOUTUNADJ / DTOUTADJ)
        /// </summary>
        /// <param name="pDbTransaction">Transaction en cours</param>
        /// <param name="pTableWork">Table de travail des principaux événements candidats à mise à jour</param>
        /// EG 20220825 [26083][WI412] Réécriture du traitement de mise à jour des dates d'échéance sur les tables TRADE, EVENT et tables liées 
        private void UpdateTradeWithMaturity(IDbTransaction pDbTransaction, string pTableWork)
        {
            string sqlUpdate = string.Empty;
            if (DataHelper.IsDbSqlServer(Cs))
            {
                sqlUpdate = @"update tr set DTOUTUNADJ = ev.DTENDUNADJ, DTOUTADJ = ev.DTENDADJ 
                from dbo.{0} tw
                inner join dbo.EVENT ev on (ev.IDE = tw.IDE) 
                inner join dbo.TRADE tr on (tr.IDT = ev.IDT)
                where (tw.ORIGIN = 'P') and (tw.EVENTCODE = @EVENTCODE) and (tw.EVENTTYPE = @EVENTTYPE) and (ev.INSTRUMENTNO = 1)";

            }
            else if (DataHelper.IsDbOracle(Cs))
            {
                sqlUpdate = @"update dbo.TRADE set (DTOUTUNADJ, DTOUTADJ) =
                (select ev.DTENDUNADJ, ev.DTENDADJ
                from dbo.{0} tw
                inner join dbo.EVENT ev on (ev.IDE = tw.IDE) 
                where (tw.IDT = TRADE.IDT) and (tw.ORIGIN = 'P') and (tw.EVENTCODE = @EVENTCODE) and (tw.EVENTTYPE = @EVENTTYPE) and (ev.INSTRUMENTNO = 1))
                where exists ( select 1 from dbo.{0} tw 
                inner join dbo.EVENT ev on (ev.IDE = tw.IDE) 
                where (tw.IDT = TRADE.IDT) and (tw.ORIGIN = 'P') and (tw.EVENTCODE = @EVENTCODE) and (tw.EVENTTYPE = @EVENTTYPE) and (ev.INSTRUMENTNO = 1));"; 
            }

            sqlUpdate = String.Format(sqlUpdate, pTableWork);

            DataParameters dp = new DataParameters();
            dp.Add(DataParameter.GetParameter(Cs, DataParameter.ParameterEnum.EVENTCODE), EventCodeFunc.Product);
            dp.Add(DataParameter.GetParameter(Cs, DataParameter.ParameterEnum.EVENTTYPE), EventTypeFunc.Date);
            QueryParameters qryParameters = new QueryParameters(Cs, sqlUpdate, dp);
            DataHelper.ExecuteNonQuery(pDbTransaction, CommandType.Text, qryParameters.Query, qryParameters.Parameters.GetArrayDbParameter());
        }
        /// <summary>
        /// Mise à jour de la table TRADEINSTRUMENT (DTOUTUNADJ / DTOUTADJ)
        /// </summary>
        /// <param name="pDbTransaction">Transaction en cours</param>
        /// <param name="pTableWork">Table de travail des principaux événements candidats à mise à jour</param>
        /// EG 20220825 [26083][WI412] Réécriture du traitement de mise à jour des dates d'échéance sur les tables TRADE, EVENT et tables liées 
        private void UpdateTradeInstrumentWithMaturity(IDbTransaction pDbTransaction, string pTableWork)
        {
            string sqlUpdate = string.Empty;
            if (DataHelper.IsDbSqlServer(Cs))
            {
                sqlUpdate = @"update ti set DTOUTUNADJ = ev.DTENDUNADJ, DTOUTADJ = ev.DTENDADJ 
                from dbo.{0} tw
                inner join dbo.EVENT ev on (ev.IDE = tw.IDE) 
                inner join dbo.TRADEINSTRUMENT ti on (ti.IDT = ev.IDT) and (ti.INSTRUMENTNO = ev.INSTRUMENTNO)
                where (tw.ORIGIN = 'P') and (tw.EVENTCODE = @EVENTCODE) and (tw.EVENTTYPE = @EVENTTYPE)";

            }
            else if (DataHelper.IsDbOracle(Cs))
            {
                sqlUpdate = @"update dbo.TRADEINSTRUMENT set (DTOUTUNADJ, DTOUTADJ) =
                (select ev.DTENDUNADJ, ev.DTENDADJ
                from dbo.{0} tw
                inner join dbo.EVENT ev on (ev.IDE = tw.IDE) 
                inner join dbo.TRADEINSTRUMENT ti on (ti.IDT = ev.IDT) and (ti.INSTRUMENTNO = ev.INSTRUMENTNO)
                where (tw.IDT = TRADEINSTRUMENT.IDT) and (tw.ORIGIN = 'P') and (tw.EVENTCODE = @EVENTCODE) and (tw.EVENTTYPE = @EVENTTYPE))
                where exists ( select 1 from dbo.{0} tw where (tw.IDT = TRADEINSTRUMENT.IDT) and (tw.ORIGIN = 'P') and (tw.EVENTCODE = @EVENTCODE) and (tw.EVENTTYPE = @EVENTTYPE));";
            }

            sqlUpdate = String.Format(sqlUpdate, pTableWork);

            DataParameters dp = new DataParameters();
            dp.Add(DataParameter.GetParameter(Cs, DataParameter.ParameterEnum.EVENTCODE), EventCodeFunc.Product);
            dp.Add(DataParameter.GetParameter(Cs, DataParameter.ParameterEnum.EVENTTYPE), EventTypeFunc.Date);
            QueryParameters qryParameters = new QueryParameters(Cs, sqlUpdate, dp);
            DataHelper.ExecuteNonQuery(pDbTransaction, CommandType.Text, qryParameters.Query, qryParameters.Parameters.GetArrayDbParameter());
        }

        /// <summary>
        /// Mise à jour de la table TRADESTREAM (DTTERMINATIONUNADJ / DTTERMINATIONADJ / DTOUTUNADJ / DTOUTADJ)
        /// </summary>
        /// <param name="pDbTransaction">Transaction en cours</param>
        /// <param name="pTableWork">Table de travail des principaux événements candidats à mise à jour</param>
        /// EG 20220825 [26083][WI412] Réécriture du traitement de mise à jour des dates d'échéance sur les tables TRADE, EVENT et tables liées 
        private void UpdateTradeStreamWithMaturity(IDbTransaction pDbTransaction, string pTableWork)
        {
            string sqlUpdate = string.Empty;
            if (DataHelper.IsDbSqlServer(Cs))
            {
                sqlUpdate = @"update ts set DTTERMINATIONUNADJ = ev.DTENDUNADJ, DTTERMINATIONADJ = ev.DTENDADJ, DTOUTUNADJ = ev.DTENDUNADJ, DTOUTADJ = ev.DTENDADJ
                from dbo.{0} tw
                inner join dbo.EVENT ev on (ev.IDE = tw.IDE) 
                inner join dbo.TRADESTREAM ts on (ts.IDT = ev.IDT) and (ts.INSTRUMENTNO = ev.INSTRUMENTNO) and (ts.STREAMNO = ev.STREAMNO)
                where (tw.ORIGIN = 'S') and (tw.EVENTCODE in ('FED','AED','EED'))";

            }
            else if (DataHelper.IsDbOracle(Cs))
            {
                sqlUpdate = @"update dbo.TRADESTREAM set (DTTERMINATIONUNADJ, DTTERMINATIONADJ, DTOUTUNADJ, DTOUTADJ) =
                (select ev.DTENDUNADJ, ev.DTENDADJ, ev.DTENDUNADJ, ev.DTENDADJ
                from dbo.{0} tw
                inner join dbo.EVENT ev on (ev.IDE = tw.IDE) 
                where (tw.IDT = TRADESTREAM.IDT) and (ev.INSTRUMENTNO = TRADESTREAM.INSTRUMENTNO) and (ev.STREAMNO = TRADESTREAM.STREAMNO) 
                and (tw.ORIGIN = 'S') and (tw.EVENTCODE in ('FED','AED','EED')))
                where exists (select 1 
                from dbo.{0} tw 
                inner join dbo.EVENT ev on (ev.IDE = tw.IDE) 
                where (tw.IDT = TRADESTREAM.IDT) and (ev.INSTRUMENTNO = TRADESTREAM.INSTRUMENTNO) and (ev.STREAMNO = TRADESTREAM.STREAMNO) 
                and (tw.ORIGIN = 'S') and (tw.EVENTCODE in ('FED','AED','EED')));";
            }

            sqlUpdate = String.Format(sqlUpdate, pTableWork);
            DataHelper.ExecuteNonQuery(pDbTransaction, CommandType.Text, sqlUpdate);
        }

        /// <summary>
        /// Mise à jour de la table TRADE (DTOUT)
        /// </summary>
        /// <param name="pDbTransaction">Transaction en cours</param>
        /// <param name="pTableWork">Table de travail des principaux événements candidats à mise à jour</param>
        /// EG 20220825 [26083][WI412] Réécriture du traitement de mise à jour des dates d'échéance sur les tables TRADE, EVENT et tables liées 
        private void UpdateTradeDTOUTWithMaturity(IDbTransaction pDbTransaction, int pIdMaturity, DateTime pMaturityDateSys)
        {
            string sqlUpdate = string.Empty;
            if (DataHelper.IsDbSqlServer(Cs))
            {
                sqlUpdate = String.Format(@"update tr set DTOUT = {0}
                from dbo.TRADE tr
                inner join dbo.ASSET_ETD ass on (ass.IDASSET = tr.IDASSET)
                inner join dbo.DERIVATIVEATTRIB da on (da.IDDERIVATIVEATTRIB = ass.IDDERIVATIVEATTRIB)
                inner join dbo.MATURITY ma on (ma.IDMATURITY = da.IDMATURITY) and (ma.IDMATURITY = @IDMATURITY)
                where ({1}) and (tr.IDSTACTIVATION = 'REGULAR') and (tr.IDSTBUSINESS = 'ALLOC') and (tr.ASSETCATEGORY = 'ExchangeTradedContract')",
                DtFunc.IsDateTimeFilled(pMaturityDateSys) ? "DATEADD(day, 63, @MATURITYDATE)" : "null",
                DtFunc.IsDateTimeFilled(pMaturityDateSys) ? "(tr.DTOUT is null) or (tr.DTOUT != DATEADD(day, 63, @MATURITYDATE))" : "tr.DTOUT is not null");

            }
            else if (DataHelper.IsDbOracle(Cs))
            {
                sqlUpdate = String.Format(@"update dbo.TRADE set DTOUT =
                (
                    select {0}
                    from dbo.TRADE tr
                    inner join dbo.ASSET_ETD ass on (ass.IDASSET = tr.IDASSET)
                    inner join dbo.DERIVATIVEATTRIB da on (da.IDDERIVATIVEATTRIB = ass.IDDERIVATIVEATTRIB)
                    inner join dbo.MATURITY ma on (ma.IDMATURITY = da.IDMATURITY) and (ma.IDMATURITY = @IDMATURITY)
                    where (tr.IDT = TRADE.IDT) and (tr.IDSTACTIVATION = 'REGULAR') and (tr.IDSTBUSINESS = 'ALLOC') and (tr.ASSETCATEGORY = 'ExchangeTradedContract')
                )
                where exists 
                (
                    select 1
                    from dbo.TRADE tr
                    inner join dbo.ASSET_ETD ass on (ass.IDASSET = tr.IDASSET) 
                    inner join dbo.DERIVATIVEATTRIB da on (da.IDDERIVATIVEATTRIB = ass.IDDERIVATIVEATTRIB)
                    inner join dbo.MATURITY ma on (ma.IDMATURITY = da.IDMATURITY) and (ma.IDMATURITY = @IDMATURITY)
                    where (tr.IDT = TRADE.IDT) and ({1}) and (tr.IDSTACTIVATION = 'REGULAR') and (tr.IDSTBUSINESS = 'ALLOC') and (tr.ASSETCATEGORY = 'ExchangeTradedContract')
                )",
                DtFunc.IsDateTimeFilled(pMaturityDateSys) ? "@MATURITYDATE + 63" : "null",
                DtFunc.IsDateTimeFilled(pMaturityDateSys) ? "(tr.DTOUT is null) or (tr.DTOUT != @MATURITYDATE + 63)" : "tr.DTOUT is not null");
            }

            DataParameters dp = new DataParameters();
            dp.Add(DataParameter.GetParameter(Cs, DataParameter.ParameterEnum.IDMATURITY), pIdMaturity);
            dp.Add(DataParameter.GetParameter(Cs, DataParameter.ParameterEnum.MATURITYDATE), pMaturityDateSys);
            QueryParameters qryParameters = new QueryParameters(Cs, sqlUpdate, dp);
            DataHelper.ExecuteNonQuery(pDbTransaction, CommandType.Text, qryParameters.Query, qryParameters.Parameters.GetArrayDbParameter());
        }

        /// <summary>
        /// Mise à jour de la table POSACTION  (DTOUT)
        /// </summary>
        /// <param name="pDbTransaction">Transaction en cours</param>
        /// <param name="pTableWork">Table de travail des principaux événements candidats à mise à jour</param>
        /// EG 20220825 [26083][WI412] Réécriture du traitement de mise à jour des dates d'échéance sur les tables TRADE, EVENT et tables liées 
        private void UpdatePosactionWithMaturity(IDbTransaction pDbTransaction, int pIdMaturity, DateTime pMaturityDateSys)
        {
            string sqlUpdate = string.Empty;
            if (DataHelper.IsDbSqlServer(Cs))
            {
                sqlUpdate = String.Format(@"update pa set DTOUT = {0}
                from dbo.POSACTION pa
                inner join dbo.POSACTIONDET pad on (pad.IDPA = pa.IDPA)
                inner join dbo.TRADE tr on (tr.IDT = pad.IDT_CLOSING)
                inner join dbo.ASSET_ETD ass on (ass.IDASSET = tr.IDASSET)
                inner join dbo.DERIVATIVEATTRIB da on (da.IDDERIVATIVEATTRIB = ass.IDDERIVATIVEATTRIB)
                inner join dbo.MATURITY ma on (ma.IDMATURITY = da.IDMATURITY) and (ma.IDMATURITY = @IDMATURITY)
                where ({1})  and (tr.IDSTACTIVATION = 'REGULAR') and (tr.IDSTBUSINESS = 'ALLOC') and (tr.ASSETCATEGORY = 'ExchangeTradedContract')",
                DtFunc.IsDateTimeFilled(pMaturityDateSys) ? "DATEADD(day, 63, @MATURITYDATE)" : "null",
                DtFunc.IsDateTimeFilled(pMaturityDateSys) ? "(pa.DTOUT is null) or (pa.DTOUT != DATEADD(day, 63, @MATURITYDATE))" : "pa.DTOUT is not null");
            }
            else if (DataHelper.IsDbOracle(Cs))
            {
                sqlUpdate = String.Format(@"update dbo.POSACTION set DTOUT =
                (
                    select {0}
                    from dbo.POSACTION pa
                    inner join dbo.POSACTIONDET pad on (pad.IDPA = pa.IDPA)
                    inner join dbo.TRADE tr on (tr.IDT = pad.IDT_CLOSING)
                    inner join dbo.ASSET_ETD ass on (ass.IDASSET = tr.IDASSET) 
                    inner join dbo.DERIVATIVEATTRIB da on (da.IDDERIVATIVEATTRIB = ass.IDDERIVATIVEATTRIB)
                    inner join dbo.MATURITY ma on (ma.IDMATURITY = da.IDMATURITY) and (ma.IDMATURITY = @IDMATURITY)
                    where (pa.IDPA = POSACTION.IDPA) and (tr.IDSTACTIVATION = 'REGULAR') and (tr.IDSTBUSINESS = 'ALLOC') and (tr.ASSETCATEGORY = 'ExchangeTradedContract')  
                )
                where exists
                (
                    select 1
                    from dbo.POSACTION pa
                    inner join dbo.POSACTIONDET pad on (pad.IDPA = pa.IDPA)
                    inner join dbo.TRADE tr on (tr.IDT = pad.IDT_CLOSING)
                    inner join dbo.ASSET_ETD ass on (ass.IDASSET = tr.IDASSET)
                    inner join dbo.DERIVATIVEATTRIB da on (da.IDDERIVATIVEATTRIB = ass.IDDERIVATIVEATTRIB)
                    inner join dbo.MATURITY ma on (ma.IDMATURITY = da.IDMATURITY) and (ma.IDMATURITY = @IDMATURITY)
                    where (pa.IDPA = POSACTION.IDPA) and ({1}) and (tr.IDSTACTIVATION = 'REGULAR') and (tr.IDSTBUSINESS = 'ALLOC') and (tr.ASSETCATEGORY = 'ExchangeTradedContract')
                )",
                DtFunc.IsDateTimeFilled(pMaturityDateSys) ? "@MATURITYDATE + 63" : "null",
                DtFunc.IsDateTimeFilled(pMaturityDateSys) ? "(pa.DTOUT is null) or (pa.DTOUT != @MATURITYDATE + 63)" : "pa.DTOUT is not null");
            }

            DataParameters dp = new DataParameters();
            dp.Add(DataParameter.GetParameter(Cs, DataParameter.ParameterEnum.IDMATURITY), pIdMaturity);
            dp.Add(DataParameter.GetParameter(Cs, DataParameter.ParameterEnum.MATURITYDATE), pMaturityDateSys);
            QueryParameters qryParameters = new QueryParameters(Cs, sqlUpdate, dp);
            DataHelper.ExecuteNonQuery(pDbTransaction, CommandType.Text, qryParameters.Query, qryParameters.Parameters.GetArrayDbParameter());
        }

        /// <summary>
        /// Charge les dépôts de garantie titre impactés par la cotation et poste les messages vers EVENTSVAL   
        /// </summary>
        /// <returns></returns>
        private Cst.ErrLevel ProcessExecuteQuotationCollateral()
        {
            Cst.ErrLevel ret = Cst.ErrLevel.NOTHINGTODO;

            Nullable<Cst.UnderlyingAsset> underlyingAsset = null;
            if (Quote.QuoteTable == Cst.OTCml_TBL.QUOTE_DEBTSEC_H.ToString())
                underlyingAsset = Cst.UnderlyingAsset.Bond;
            else if (Quote.QuoteTable == Cst.OTCml_TBL.QUOTE_EQUITY_H.ToString())
                underlyingAsset = Cst.UnderlyingAsset.EquityAsset;

            if (underlyingAsset.HasValue)
            {
                LoadCollateral(underlyingAsset.Value);
                if (0 < _ds.Tables[0].Rows.Count)
                {
                    ret = Cst.ErrLevel.SUCCESS;
                    foreach (DataRow row in _ds.Tables[0].Rows)
                    {
                        MQueueAttributes mQueueAttributes = new MQueueAttributes()
                        {
                            connectionString  = Cs,
                            id = Convert.ToInt32(row["IDPOSCOLLATERAL"]),
                            idInfo = new IdInfo()
                            {
                                id = Convert.ToInt32(row["IDPOSCOLLATERAL"]),
                                idInfos = new DictionaryEntry[] { new DictionaryEntry("ident", "COLLATERAL") }
                            },
                            requester = _quotationHandlingMQueue.header.requester
                        };
                        
                        CollateralValMQueue collateralValMQueue = new CollateralValMQueue(Quote, mQueueAttributes);
                        SendEventsValService(collateralValMQueue);
                    }
                }
                else
                {
                    
                    Logger.Log(new LoggerData(LogLevelEnum.Info, new SysMsgCode(SysCodeEnum.LOG, 407), 0,
                        new LogParam(CurrentId, default, default, Cst.LoggerParameterLink.IDDATA)));
                }
            }
            return ret;
        }

        /// <summary>
        /// Charge les dépôt de garantie titre impactés par la cotation 
        /// <para>Alimente le dataset _ds</para>
        /// </summary>
        // RD 20160331 [23036] Remplacement du paramètre DATE par DTQUOTE
        private void LoadCollateral(Cst.UnderlyingAsset pUnderlyingAsset)
        {
            DataParameters parameters = new DataParameters();
            parameters.Add(new DataParameter(Cs, "IDASSET", DbType.Int32), Quote.idAsset);
            parameters.Add(new DataParameter(Cs, "ASSETCATEGORY", DbType.AnsiString, 64), pUnderlyingAsset);
            parameters.Add(new DataParameter(Cs, "DTQUOTE", DbType.Date), Quote.time.Date);

            string sqlSelect = @"select IDPOSCOLLATERAL
            from dbo.POSCOLLATERAL co
            where (co.ASSETCATEGORY = @ASSETCATEGORY) and (co.IDASSET = @IDASSET) and 
            ( 
              ((co.DTBUSINESS = @DTQUOTE) and (co.DURATION = 'OVN')) or
              ((co.DTBUSINESS <= @DTQUOTE) and (co.DURATION != 'OVN') and ((co.DTTERMINATION is null) or (co.DTTERMINATION > @DTQUOTE)))
            )";

            QueryParameters qryParameters = new QueryParameters(Cs, sqlSelect.ToString(), parameters);
            _ds = DataHelper.ExecuteDataset(Cs, CommandType.Text, qryParameters.Query, parameters.GetArrayDbParameter());
        }

        /// <summary>
        /// Charge les trades impactés par la cotation et poste les messages vers EVENTSVAL   
        /// </summary>
        /// <returns></returns>
        /// EG 20140801 New  
        /// FI 20170306 [22225] Modify
        private Cst.ErrLevel ProcessExecuteQuotationTrade()
        {
            Cst.ErrLevel ret = Cst.ErrLevel.NOTHINGTODO;
            bool isNothingToDo = false;

            if (Quote is Quote_ETDAsset)
            {
                Quote_ETDAsset quote_ETDAsset = Quote as Quote_ETDAsset;

                // FI 20170306 [22225] call CheckQuote_ETDAsset
                CheckQuote_ETDAsset(quote_ETDAsset);

                ret = Position_ETD();
                isNothingToDo = (Cst.ErrLevel.NOTHINGTODO == ret);
                
                if (quote_ETDAsset.isCashFlowsValSpecified && quote_ETDAsset.isCashFlowsVal) 
                {
                    // FI 20170306 [22225] Appel à CalcTradeFees (Recehrche des trades ayant des frais 
                    ret = CalcTradeFees();
                    isNothingToDo = isNothingToDo && (Cst.ErrLevel.NOTHINGTODO == ret);
                }
            }
            else
            {
                if ((Quote is Quote_Equity) || (Quote is Quote_Index))
                {
                    ret = Position_OTC();
                    isNothingToDo = (Cst.ErrLevel.NOTHINGTODO == ret);
                }

                if ((ret == Cst.ErrLevel.SUCCESS) || (ret == Cst.ErrLevel.NOTHINGTODO) || isNothingToDo)
                {
                    ret = Trades_OTC();
                    isNothingToDo = isNothingToDo && (Cst.ErrLevel.NOTHINGTODO == ret);
                }
            }

            if (isNothingToDo)
            {
                Logger.Log(new LoggerData(LogLevelEnum.Info, new SysMsgCode(SysCodeEnum.LOG, 406), 0,
                    new LogParam(CurrentId, default, default, Cst.LoggerParameterLink.IDDATA)));
            }
            return ret;
        }

        /// <summary>
        /// Contrôle la cohérence de {quote_ETDAsset}
        /// </summary>
        /// <exception cref="NotSupportedException si quote_ETDAsset n'est pas ok"
        /// FI 20170306 [22225] Add
        private static void CheckQuote_ETDAsset(Quote_ETDAsset quote_ETDAsset)
        {
            if (quote_ETDAsset.IsQuoteTable_ETD)
            {
                if ((quote_ETDAsset.QuoteTable == Cst.OTCml_TBL.DERIVATIVECONTRACT.ToString()) && (false == quote_ETDAsset.idDCSpecified))
                    throw new NotSupportedException("No Derivative contract specified");

                if ((quote_ETDAsset.QuoteTable == Cst.OTCml_TBL.DERIVATIVEATTRIB.ToString()) && (false == quote_ETDAsset.idDerivativeAttribSpecified))
                    throw new NotSupportedException("No DeriveAttrib specified");

                if ((quote_ETDAsset.QuoteTable == Cst.OTCml_TBL.ASSET_ETD.ToString()) && (quote_ETDAsset.idAsset == 0))
                    throw new NotSupportedException("No asset specified");
            }
        }


        /// <summary>
        ///  Requête de selection des allocations ETD en position à la date de quotation
        /// </summary>
        /// <returns></returns>
        /// EG 20141224 [20566]
        /// FI 20170306 [22225] Modify
        /// EG 20170412 [23081] Refactoring GetQueryPositionActionBySide replace by GetQryPosAction_BySide
        // EG 20191115 [25077] RDBMS : New version of Trades tables architecture (TRADESTSYS merge to TRADE, NEW TABLE TRADEXML)
        // EG 20200226 Refactoring suite à à TRADEINSTRUMENT (INSTRUMENTNO=1) dans TRADE
        // EG 20201006 [25350] Via QUOTEHANDLING IDEM est passé en paramètre dans le message EVENTSVAL (colonne restituée dans les queries)
        private Cst.ErrLevel Position_ETD()
        {
            // FI 20170306 [22225] Valorisation de quoteEtd
            if (!(Quote is Quote_ETDAsset quoteEtd))
                throw new NotSupportedException("quote is not an ETD quote");

            string sqlSelect = string.Empty;

            // FI 20190704 [24745]  divers modification dans la requête 
            // - Ajout de restriction sur DTOUT
            // - Ne pas considérer les assets option à l'échéance
            // - Considérer les asset Enabled uniquement
            DataParameters dp = new DataParameters();
            if (Cst.OTCml_TBL.QUOTE_ETD_H.ToString() == quoteEtd.QuoteTable)
            {
                // Mode classique de modification du prix d'un ETD
                dp.Add(DataParameter.GetParameter(Cs,DataParameter.ParameterEnum.DTBUSINESS), quoteEtd.time.Date); // FI 20201006 [XXXXX] DbType.Date
                dp.Add(new DataParameter(Cs, "IDASSET", DbType.Int32), quoteEtd.idAsset);

                sqlSelect += StrFunc.AppendFormat(@"select tr.IDT, tr.IDENTIFIER, tr.DTBUSINESS, 'FUT' as GPRODUCT, em.IDEM
                from dbo.TRADE tr
                inner join dbo.MARKET mk on (mk.IDM = tr.IDM)
                inner join dbo.VW_INSTR_PRODUCT pr on ( pr.IDI = tr.IDI) and (pr.FUNGIBILITYMODE != 'NONE') and (pr.GPRODUCT = 'FUT')
                inner join dbo.BOOK bd on (bd.IDB = tr.IDB_DEALER)
                inner join dbo.BOOK bc on (bc.IDB = tr.IDB_CLEARER)
                inner join dbo.ENTITYMARKET em on ( em.IDM = tr.IDM ) and (em.IDA = bd.IDA_ENTITY) and (em.IDA_CUSTODIAN is null) and (em.DTENTITY = @DTBUSINESS)
                inner join dbo.ASSET_ETD ass on (ass.IDASSET = tr.IDASSET) and ({0})
                inner join dbo.DERIVATIVEATTRIB da on (da.IDDERIVATIVEATTRIB = ass.IDDERIVATIVEATTRIB)
                inner join dbo.DERIVATIVECONTRACT dc on (dc.IDDC = da.IDDC)
                inner join dbo.MATURITY ma on (ma.IDMATURITY = da.IDMATURITY) and 
                (
                (isnull(ma.MATURITYDATESYS,ma.MATURITYDATE) is null) or 
                (isnull(ma.MATURITYDATESYS, ma.MATURITYDATE)> @DTBUSINESS) or
                ((dc.CATEGORY='F') and (isnull(ma.MATURITYDATESYS,ma.MATURITYDATE)=@DTBUSINESS))
                )", OTCmlHelper.GetSQLDataDtEnabled(Cs,"ass",  quoteEtd.time)) + Cst.CrLf;


                // Achats Clôturés
                sqlSelect += @"left outer join (" + Cst.CrLf + PosKeepingTools.GetQryPosAction_BySide(BuyerSellerEnum.BUYER) + ") pab on (pab.IDT = tr.IDT)" + Cst.CrLf;
                // Ventes Clôturées
                sqlSelect += @"left outer join (" + Cst.CrLf + PosKeepingTools.GetQryPosAction_BySide(BuyerSellerEnum.SELLER) + ") pas on (pas.IDT = tr.IDT)" + Cst.CrLf;

                //─────────────────────────────────────────────────────────────────────────────────────────────────────────
                // Clause WHERE : On ne charge que les Trades en position OU les Trades du JOUR 
                //─────────────────────────────────────────────────────────────────────────────────────────────────────────
                sqlSelect += @"where 
                (tr.DTBUSINESS <= @DTBUSINESS) and (tr.IDSTACTIVATION = 'REGULAR') and (tr.IDSTBUSINESS = 'ALLOC') and (tr.IDASSET = @IDASSET) and
                ((tr.QTY - isnull(pab.QTY, 0) - isnull(pas.QTY, 0) > 0)  or (tr.DTBUSINESS = em.DTENTITY)) and
                (tr.DTOUT is null or tr.DTOUT > @DTBUSINESS)" + Cst.CrLf;

                // PM 20130222 [18414] Ajout de la position résultante d'un Cascading dans la journée de bourse en cours
                // PM 20150601 [20575] Utilisation de DTENTITY au lieu de DTMARKET
                // RD 20160331 [23030] Remplacement du paramètre ASSETCATEGORY par "ExchangeTradedContract".
                sqlSelect += @"union all 
                select tr.IDT, tr.IDENTIFIER, tr.DTBUSINESS, 'FUT' as GPRODUCT, tr.IDEM
                from dbo.VW_TRADE_POSETD tr
                inner join dbo.TRADELINK tl on (tl.IDT_A = tr.IDT) and (tl.LINK = 'PositionAfterCascading')
                inner join dbo.EVENT ev on (ev.IDT = tl.IDT_B) and (ev.EVENTCODE = 'CAS')
                inner join dbo.EVENTCLASS ec on (ec.IDE = ev.IDE) and (ec.EVENTCLASS = 'GRP') and (ec.DTEVENT = @DTBUSINESS)
                inner join dbo.EVENT evroot on (evroot.IDE = ev.IDE_EVENT)
                inner join dbo.EVENTASSET ea on (ea.IDE = evroot.IDE) and (ea.IDASSET = @IDASSET) and (ea.ASSETCATEGORY = 'ExchangeTradedContract')
                where (tr.DTBUSINESS = @DTBUSINESS) and (tr.DTENTITY = @DTBUSINESS)" + Cst.CrLf;

                //─────────────────────────────────────────────────────────────────────────────────────────────────────────
                // Ajout Trade sous-jacent avec Settltement Currency
                //─────────────────────────────────────────────────────────────────────────────────────────────────────────
                // PM 20150601 [20575] Utilisation de DTENTITY au lieu de DTMARKET
                sqlSelect += @"union 
                select tr.IDT, tr.IDENTIFIER, tr.DTBUSINESS, 'FUT' as GPRODUCT, tr.IDEM
                from dbo.VW_TRADE_POSETD tr
                inner join dbo.EVENT ev on (ev.IDT = tr.IDT) and (ev.EVENTTYPE = 'SCU')
                inner join dbo.EVENTCLASS ec on (ec.IDE = ev.IDE) and (ec.EVENTCLASS = 'REC') and (ec.DTEVENT = @DTBUSINESS)
                inner join dbo.EVENTASSET ea on (ea.IDE = ev.IDE) and (ea.IDASSET = @IDASSET) and (ea.ASSETCATEGORY = 'ExchangeTradedContract')
                inner join dbo.EVENT evroot on (evroot.IDE = ev.IDE_EVENT) and (evroot.EVENTCODE in ('MOF', 'AEX', 'AAS', 'ASS', 'EXE'))
                where (tr.DTBUSINESS = @DTBUSINESS) and (tr.DTENTITY = @DTBUSINESS)" + Cst.CrLf;


                // PM 20130222 [18414] Ajout de la position sur laquelle à eu lieu le Cascading dans la journée de bourse en cours
                // lorsqu'il s'agit de position issu de trades antérieures à la journée de bourse en cours
                // PM 20150601 [20575] Utilisation de DTENTITY au lieu de DTMARKET
                sqlSelect += @"union all 
                select tr.IDT, tr.IDENTIFIER, tr.DTBUSINESS, 'FUT' as GPRODUCT, tr.IDEM
                from dbo.VW_TRADE_POSETD tr
                inner join dbo.EVENT ev on (ev.IDT = tr.IDT) and (ev.EVENTTYPE = 'CAS')
                inner join dbo.EVENTCLASS ec on (ec.IDE = ev.IDE) and (ec.EVENTCLASS = 'GRP') and (ec.DTEVENT = @DTBUSINESS)
                inner join dbo.EVENT evroot on (evroot.IDE = ev.IDE_EVENT)
                inner join dbo.EVENTASSET ea on (ea.IDE = evroot.IDE) and (ea.IDASSET = @IDASSET) and (ea.ASSETCATEGORY = 'ExchangeTradedContract')
                where (tr.DTBUSINESS < @DTBUSINESS) and (tr.DTENTITY = @DTBUSINESS)" + Cst.CrLf;
            }
            else
            {
                Boolean isDERIVATIVECONTRACT = (quoteEtd.QuoteTable == Cst.OTCml_TBL.DERIVATIVECONTRACT.ToString());
                Boolean isDERIVATIVEATTRIB = (quoteEtd.QuoteTable == Cst.OTCml_TBL.DERIVATIVEATTRIB.ToString());
                Boolean isASSET = (quoteEtd.QuoteTable == Cst.OTCml_TBL.ASSET_ETD.ToString());

                bool isDtBusinessFilled = DtFunc.IsDateTimeFilled(quoteEtd.time);
                if (isDtBusinessFilled)
                {
                    dp.Add(DataParameter.GetParameter(Cs, DataParameter.ParameterEnum.DTBUSINESS), quoteEtd.time.Date);
                }
                if (isDERIVATIVECONTRACT)
                {
                    dp.Add(new DataParameter(Cs, "IDDC", DbType.Int32), quoteEtd.idDC);
                }
                else if (isDERIVATIVEATTRIB)
                {
                    dp.Add(DataParameter.GetParameter(Cs, DataParameter.ParameterEnum.IDDERIVATIVEATTRIB), quoteEtd.idDerivativeAttrib);
                }
                else if (isASSET)
                {
                    dp.Add(new DataParameter(Cs, "IDASSET", DbType.Int32), quoteEtd.idAsset);
                }


                // PM 20151027 [20964] Ajout DTENTITY car il faut utiliser Max(DTBUSINESS, DTENTITY) comme date de début des événements à recalculer
                sqlSelect = @"select tr.IDT, tr.IDENTIFIER, tr.DTBUSINESS, 'FUT' as GPRODUCT, tr.DTENTITY, tr.IDEM" + Cst.CrLf;
                sqlSelect += @"  from dbo.VW_TRADE_POSETD tr" + Cst.CrLf;
                // Jointure pour faire le lien avec le DERIVATIVECONTRACT ou le DERIVATIVEATTRIB
                if (isDERIVATIVECONTRACT || isDERIVATIVEATTRIB)
                {
                    sqlSelect += @"inner join dbo.VW_ASSET_ETD_EXPANDED aetd on (aetd.IDASSET = tr.IDASSET)" + Cst.CrLf;
                }

                // Achats/Ventes Clôturés
                if (isDtBusinessFilled)
                {
                    sqlSelect += @"left outer join (" + PosKeepingTools.GetQryPosAction_BySide(BuyerSellerEnum.BUYER) + ") pab on (pab.IDT = tr.IDT)" + Cst.CrLf;
                    sqlSelect += @"left outer join (" + PosKeepingTools.GetQryPosAction_BySide(BuyerSellerEnum.SELLER) + ") pas on (pas.IDT = tr.IDT)" + Cst.CrLf;
                    // PM 20151027 [20964] Prendre également les trades entrés en avance
                    //sqlSelect += @"where (tr.DTBUSINESS <= @DTBUSINESS) and (tr.DTENTITY = @DTBUSINESS)" + Cst.CrLf;
                    sqlSelect += @"where (tr.DTENTITY <= @DTBUSINESS)" + Cst.CrLf;
                    sqlSelect += @"and " + Cst.CrLf;
                }
                else
                {
                    // PM 20151027 [21491] Utilisation de DTENTITY au lieu de DTMARKET
                    //sqlSelect += @"left outer join (" + PosKeepingTools.GetQueryPositionActionBySideForDtMarket(Cs, BuyerSellerEnum.BUYER) + ") pab on (pab.IDT = tr.IDT)" + Cst.CrLf;
                    //sqlSelect += @"left outer join (" + PosKeepingTools.GetQueryPositionActionBySideForDtMarket(Cs, BuyerSellerEnum.SELLER) + ") pas on (pas.IDT = tr.IDT)" + Cst.CrLf;
                    sqlSelect += @"left outer join (" + PosKeepingTools.GetQueryPositionActionBySideForDtEntity(Cs, BuyerSellerEnum.BUYER) + ") pab on (pab.IDT = tr.IDT)" + Cst.CrLf;
                    sqlSelect += @"left outer join (" + PosKeepingTools.GetQueryPositionActionBySideForDtEntity(Cs, BuyerSellerEnum.SELLER) + ") pas on (pas.IDT = tr.IDT)" + Cst.CrLf;
                    // PM 20151027 [20964] Prendre également les trades entrés en avance
                    //sqlSelect += @"where (tr.DTBUSINESS <= tr.DTENTITY)" + Cst.CrLf;
                    sqlSelect += @"where " + Cst.CrLf;
                }

                // PM 20151027 [20964] Prendre également les trades entrés en avance
                //sqlSelect += @"and ((tr.QTY - isnull(pab.QTY, 0) - isnull(pas.QTY, 0) > 0)  or (tr.DTBUSINESS = tr.DTENTITY))" + Cst.CrLf;
                sqlSelect += @" ((tr.QTY - isnull(pab.QTY, 0) - isnull(pas.QTY, 0) > 0)  or (tr.DTBUSINESS >= tr.DTENTITY))" + Cst.CrLf;

                if (isDERIVATIVECONTRACT)
                {
                    sqlSelect += @"and (aetd.IDDC = @IDDC)" + Cst.CrLf;
                }
                else if (isDERIVATIVEATTRIB)
                {
                    sqlSelect += @"and (aetd.IDDERIVATIVEATTRIB = @IDDERIVATIVEATTRIB)" + Cst.CrLf;
                }
                else
                {
                    // PM 20151027 [20964] Erreur lors de la modification d'un Asset_ETD
                    //sqlSelect += @"and (tr.IDASSET = @IDASSET) and (ea.ASSETCATEGORY = @ASSETCATEGORY)" + Cst.CrLf;
                    // FI 20170306 [22225] 'ExchangeTradedContract' en dur
                    sqlSelect += @"and (tr.IDASSET = @IDASSET) and (tr.ASSETCATEGORY = 'ExchangeTradedContract')" + Cst.CrLf;
                }
            }

            QueryParameters queryParameters = new QueryParameters(Cs, sqlSelect, dp);
            _ds = DataHelper.ExecuteDataset(Cs, CommandType.Text, queryParameters.Query, queryParameters.Parameters.GetArrayDbParameter());

            // FI 20170306 [22225] valeur de retour issu de ReadTradeCandidates
            return ReadTradeCandidates();
        }

        #region Position_OTC
        /// <summary>
        ///  Traitement des allocations OTC en position à la date de quotation candidates
        /// </summary>
        /// <returns></returns>
        // EG 20140801 New
        // EG 20141224 [20566]
        // FI 20170306 [22225] Modify
        // EG 20201006 [25350] Via QUOTEHANDLING IDEM est passé en paramètre dans le message EVENTSVAL (colonne restituée dans les queries)
        private Cst.ErrLevel Position_OTC()
        {
            DataParameters parameters = new DataParameters();
            //parameters.Add(new DataParameter(Cs, "DTPOS", DbType.DateTime), quote.time.Date);
            parameters.Add(new DataParameter(Cs, "DTBUSINESS", DbType.DateTime), Quote.time.Date); // FI 20201006 [XXXXX] DbType.Date
            parameters.Add(new DataParameter(Cs, "IDASSET", DbType.Int32), Quote.idAsset);
            parameters.Add(new DataParameter(Cs, "ASSETCATEGORY", DbType.AnsiString, SQLCst.UT_ENUM_OPTIONAL_LEN), Quote.UnderlyingAsset);

            string sqlSelect = @"select tr.IDT, tr.IDENTIFIER, tr.DTBUSINESS, null as GPRODUCT, tr.IDEM
            from dbo.VW_TRADE_POSOTC tr" + Cst.CrLf;
            // Achats Clôturés
            sqlSelect += @"left outer join (" + Cst.CrLf + PosKeepingTools.GetQryPosAction_BySide(BuyerSellerEnum.BUYER) + ") pab on (pab.IDT = tr.IDT)" + Cst.CrLf;
            // Ventes Clôturées
            sqlSelect += @"left outer join (" + Cst.CrLf + PosKeepingTools.GetQryPosAction_BySide(BuyerSellerEnum.SELLER) + ") pas on (pas.IDT = tr.IDT)" + Cst.CrLf;
            // Clause WHERE Commune : On ne charge que les Trades en position OU les Trades du JOUR 
            // PM 20150601 [20575] Utilisation de DTENTITY au lieu de DTMARKET
            //            sqlSelect += @"where (tr.DTBUSINESS <= @DTBUSINESS) and (tr.IDASSET = @IDASSET) and (tr.DTMARKET = @DTBUSINESS) and (tr.ASSETCATEGORY = @ASSETCATEGORY) and
            //            ((tr.QTY - isnull(pab.QTY, 0) - isnull(pas.QTY, 0) > 0) or (tr.DTBUSINESS = tr.DTMARKET))" + Cst.CrLf;
            sqlSelect += @"where (tr.DTBUSINESS <= @DTBUSINESS) and (tr.IDASSET = @IDASSET) and (tr.DTENTITY = @DTBUSINESS) and (tr.ASSETCATEGORY = @ASSETCATEGORY) and
            ((tr.QTY - isnull(pab.QTY, 0) - isnull(pas.QTY, 0) > 0) or (tr.DTBUSINESS = tr.DTENTITY))" + Cst.CrLf;
            
            QueryParameters queryParameters = new QueryParameters(Cs, sqlSelect, parameters);
            _ds = DataHelper.ExecuteDataset(Cs, CommandType.Text, queryParameters.Query, queryParameters.Parameters.GetArrayDbParameter());

            // FI 20170306 [22225] valeur de retour issu de ReadTradeCandidates
            return ReadTradeCandidates();
        }
        #endregion Position_OTC

        #region ReadTradeCandidates
        /// <summary>
        /// Lecture des trades candidats et post pour chaque enregistrement d'un message Queue vers EventsValService
        /// </summary>
        /// <returns></returns>
        /// FI 20170306 [22225] Modify
        // EG 20201006 [25350] Via QUOTEHANDLING IDEM est passé en paramètre dans le message EVENTSVAL (colonne restituée dans les queries)
        private Cst.ErrLevel ReadTradeCandidates()
        {
            // FI 20170306 [22225] valorisation de ret
            Cst.ErrLevel ret = Cst.ErrLevel.SUCCESS;
            if (_ds.Tables[0].Rows.Count == 0)
                ret = Cst.ErrLevel.NOTHINGTODO;

            bool isETD = (Quote is Quote_ETDAsset);
            bool isQuoteTable_ETD = isETD && ((Quote_ETDAsset)Quote).IsQuoteTable_ETD;
            foreach (DataRow row in _ds.Tables[0].Rows)
            {
                string productIdentifier = "*";
                if (row.Table.Columns.Contains("PRODUCT_IDENTIFIER"))
                    productIdentifier = row["PRODUCT_IDENTIFIER"].ToString();

                // PM 20151027 [20964] Utiliser Max(DTBUSINESS,DTENTITY) comme date de début des événements à recalculer
                //if (isQuoteTable_ETD && (false == Convert.IsDBNull(row["DTBUSINESS"])))
                //{
                //    quote.time = Convert.ToDateTime(row["DTBUSINESS"]);
                //    ((Quote_ETDAsset)quote).timeSpecified = DtFunc.IsDateTimeFilled(quote.time);
                //}
                if (isQuoteTable_ETD && (false == Convert.IsDBNull(row["DTENTITY"])) && (false == Convert.IsDBNull(row["DTBUSINESS"])))
                {
                    DateTime dtEntity = Convert.ToDateTime(row["DTENTITY"]);
                    DateTime dtBusiness = Convert.ToDateTime(row["DTBUSINESS"]);
                    // Prendre Max(dtEntity,dtBusiness)
                    Quote.time = dtEntity.CompareTo(dtBusiness) > 0 ? dtEntity : dtBusiness;
                    ((Quote_ETDAsset)Quote).timeSpecified = DtFunc.IsDateTimeFilled(Quote.time);
                }

                MQueueAttributes mQueueAttributes = new MQueueAttributes()
                {
                    connectionString = Cs,
                    id = Convert.ToInt32(row["IDT"]),
                    identifier = row["IDENTIFIER"].ToString(),
                    requester = _quotationHandlingMQueue.header.requester
                };
                mQueueAttributes.idInfo = new IdInfo() { id = mQueueAttributes.id };
                if (row.Table.Columns.Contains("IDEM"))
                {
                    mQueueAttributes.idInfo.idInfos = new DictionaryEntry[] {new DictionaryEntry("ident", "TRADE"),
                                                                     new DictionaryEntry("identifier", row["IDENTIFIER"].ToString()),
                                                                     new DictionaryEntry("GPRODUCT", row["GPRODUCT"].ToString()),
                                                                     new DictionaryEntry("PRODUCT", productIdentifier),
                                                                     new DictionaryEntry("IDEM", Convert.ToInt32(row["IDEM"]))};
                }
                else
                {
                    mQueueAttributes.idInfo.idInfos = new DictionaryEntry[] {new DictionaryEntry("ident", "TRADE"),
                                                                     new DictionaryEntry("identifier", row["IDENTIFIER"].ToString()),
                                                                     new DictionaryEntry("GPRODUCT", row["GPRODUCT"].ToString()),
                                                                     new DictionaryEntry("PRODUCT", productIdentifier)};
                }

                EventsValMQueue eventsValMQueue = new EventsValMQueue(mQueueAttributes, Quote);
                SendEventsValService(eventsValMQueue);
            }

            return ret;
        }
        #endregion ReadTradeCandidates


        /// <summary>
        ///  Activation du service EventsVal
        /// </summary>
        /// <param name="pQueue"></param>
        /// <returns></returns>
        /// FI 20160325 [XXXXX] Modify
        private void SendEventsValService(MQueueBase pQueue)
        {
            
            //string message = string.Empty;
            SysMsgCode message = new SysMsgCode(SysCodeEnum.LOG, 0);
            string ident;
            string data;
            if (pQueue.IsIdT)
            {
                string gProduct = pQueue.GetStringValueIdInfoByKey("GPRODUCT");
                string productIdentifier = pQueue.GetStringValueIdInfoByKey("PRODUCT");
                switch (gProduct)
                {
                    case Cst.ProductGProduct_ADM:
                        ident = "TRADEADMIN";
                        switch (productIdentifier)
                        {
                            case "invoice":
                            case "addtionalInvoice":
                                //message = "LOG-00401";
                                message = new SysMsgCode(SysCodeEnum.LOG, 401);
                                break;
                            case "credit":
                                //message = "LOG-00402";
                                message = new SysMsgCode(SysCodeEnum.LOG, 402);
                                break;
                            case "invoiceSettlement":
                                //message = "LOG-00403";
                                message = new SysMsgCode(SysCodeEnum.LOG, 403);
                                break;
                        }
                        break;
                    case Cst.ProductGProduct_RISK:
                        //message = "LOG-00400";
                        message = new SysMsgCode(SysCodeEnum.LOG, 400);
                        ident = "TRADERISK";
                        break;
                    default:
                        //message = "LOG-00400";
                        message = new SysMsgCode(SysCodeEnum.LOG, 400);
                        ident = "TRADE";
                        break;
                }
                data = LogTools.IdentifierAndId(pQueue.identifier, pQueue.id);
            }
            else if (pQueue.GetType().Equals(typeof(CollateralValMQueue)))
            {
                ident = "COLLATERAL";
                //message = "LOG-00404";
                message = new SysMsgCode(SysCodeEnum.LOG, 404);
                data = pQueue.id.ToString();
            }
            else
            {
                throw new NotImplementedException(StrFunc.AppendFormat("Type [0] is not implemented", pQueue.GetType().ToString()));
            }

            // FI 20160325 [XXXXX] Modify => Increment 1 To column POSTEDSUBMSG (TRACKER) after Sending EVENTSVAL Message 
            Tracker.AddPostedSubMsg(1, Session);
            MQueueTools.Send(pQueue,  ServiceTools.GetMqueueSendInfo(Cst.ProcessTypeEnum.EVENTSVAL, AppInstance));

            Logger.Log(new LoggerData(LogLevelEnum.Info, message, 0,
                new LogParam(pQueue.id, default, ident, Cst.LoggerParameterLink.IDDATA),
                new LogParam(data)));
        }


        #region Trades_OTC
        /// <summary>
        /// Charge les trades où l'asset est impliqué
        /// <para>Alimente le dataset _ds</para>
        /// </summary>
        // EG 20191115 [25077] RDBMS : New version of Trades tables architecture (TRADESTSYS merge to TRADE, NEW TABLE TRADEXML)
        private Cst.ErrLevel Trades_OTC()
        {

            DataParameters parameters = new DataParameters();
            parameters.Add(new DataParameter(Cs, "IDASSET", DbType.Int32), Quote.idAsset);
            parameters.Add(new DataParameter(Cs, "EVENTCLASS", DbType.AnsiString, 3), Quote.eventClass);
            parameters.Add(DataParameter.GetParameter(Cs, DataParameter.ParameterEnum.DTEVENT), Quote.time.Date); // FI 20201006 [XXXXX] DbType.Date
            parameters.Add(new DataParameter(Cs, "ASSETCATEGORY", DbType.AnsiString, SQLCst.UT_ENUM_OPTIONAL_LEN), Quote.UnderlyingAsset);

            string sqlSelect = @"select distinct ev.IDT, tr.IDENTIFIER, pr.GPRODUCT, pr.PRODUCT_IDENTIFIER
            from dbo.EVENT ev
            inner join dbo.TRADE tr on (tr.IDT = ev.IDT)
            inner join VW_INSTR_PRODUCT pr on ( pr.IDI = tr.IDI)
            inner join dbo.EVENTCLASS ec on (ec.IDE = ev.IDE) 
            inner join EVENTASSET ea on ( ea.IDE = ev.IDE) and (ea.IDASSET = @IDASSET) and (ea.ASSETCATEGORY = @ASSETCATEGORY)
            where (ec.DTEVENT=@DTEVENT) and (ec.EVENTCLASS = @EVENTCLASS) and (tr.IDSTACTIVATION = 'REGULAR') and (tr.IDSTBUSINESS != 'ALLOC')" + Cst.CrLf;

            QueryParameters qryParameters = new QueryParameters(Cs, sqlSelect.ToString(), parameters);
            _ds = DataHelper.ExecuteDataset(Cs, CommandType.Text, qryParameters.Query, parameters.GetArrayDbParameter());

            // FI 20170306 [22225] valeur de retour issu de ReadTradeCandidates
            return ReadTradeCandidates();

        }
        #endregion Trades_OTC

        /// <summary>
        ///  <para>Recherche les trades ETD dont l'assiette s'appuie sur QuantityContractMultiplier ou premium</para>
        ///  <para>Demande pour chacun d'eux un recalcul de frais</para>
        ///  <para>seuls les trades du jour peuvent être sélectionnés</para>
        /// </summary>
        /// <returns></returns>
        /// FI 20170306 [22225] call CalcTradeFees
        // EG 20191115 [25077] RDBMS : New version of Trades tables architecture (TRADESTSYS merge to TRADE, NEW TABLE TRADEXML)
        // EG 20200226 Refactoring suite à à TRADEINSTRUMENT (INSTRUMENTNO=1) dans TRADE
        private Cst.ErrLevel CalcTradeFees()
        {
            Cst.ErrLevel ret = Cst.ErrLevel.NOTHINGTODO;

            if (!(this.Quote is Quote_ETDAsset quoteEtd))
                throw new NullReferenceException("quoteEtd is null");

            Boolean isDERIVATIVECONTRACT = (quoteEtd.QuoteTable == Cst.OTCml_TBL.DERIVATIVECONTRACT.ToString());
            Boolean isDERIVATIVEATTRIB = (quoteEtd.QuoteTable == Cst.OTCml_TBL.DERIVATIVEATTRIB.ToString());
            Boolean isASSET = (quoteEtd.QuoteTable == Cst.OTCml_TBL.ASSET_ETD.ToString());
            Boolean isQUOTE_ETD = (quoteEtd.QuoteTable == Cst.OTCml_TBL.QUOTE_ETD_H.ToString());

            string aetdRestrict = string.Empty;
            string dtBusinessRestrict = string.Empty;

            DataParameters dp = new DataParameters();
            if (isQUOTE_ETD) 
            {
                // Mode classique de modification du prix d'un ETD
                dtBusinessRestrict = "and (tr.DTBUSINESS=@DTBUSINESS)";
                dp.Add(DataParameter.GetParameter(Cs, DataParameter.ParameterEnum.DTBUSINESS), quoteEtd.time.Date); // FI 20201006 [XXXXX] DbType.Date

                aetdRestrict = "and (aetd.IDASSET=@IDASSET)";
                dp.Add(new DataParameter(Cs, "IDASSET", DbType.Int32), quoteEtd.idAsset);
            }
            else
            {
                bool isDtBusinessFilled = DtFunc.IsDateTimeFilled(quoteEtd.time);
                if (isDtBusinessFilled)
                {
                    dtBusinessRestrict = "and (tr.DTBUSINESS=@DTBUSINESS)";
                    dp.Add(DataParameter.GetParameter(Cs, DataParameter.ParameterEnum.DTBUSINESS), quoteEtd.time.Date); // FI 20201006 [XXXXX] DbType.Date
                }
                if (isDERIVATIVECONTRACT)
                {
                    aetdRestrict = "and (aetd.IDDC=@IDDC)";
                    dp.Add(new DataParameter(Cs, "IDDC", DbType.Int32), (quoteEtd.idDC));
                }
                else if (isDERIVATIVEATTRIB)
                {
                    aetdRestrict = "and (aetd.IDDERIVATIVEATTRIB=@IDDERIVATIVEATTRIB)";
                    dp.Add(DataParameter.GetParameter(Cs, DataParameter.ParameterEnum.IDDERIVATIVEATTRIB), quoteEtd.idDerivativeAttrib);
                }
                else if (isASSET || isQUOTE_ETD)
                {
                    aetdRestrict = "and (aetd.IDASSET=@IDASSET)";
                    dp.Add(new DataParameter(Cs, "IDASSET", DbType.Int32), quoteEtd.idAsset);
                }
            }

            // PL 20170403 [23015]
            // FI 20170306 [22225] La requête exclue certains trades (future livré suite à exercise '45', PositionOpening '1000', CorporateAction '1003', ...)
            // NB: Ces exclusions sont également présentes dans le traitement  "recalcul des frais"
            string sqlSelect = StrFunc.AppendFormat(@"select  
tr.IDT as T_ID, tr.IDENTIFIER as T_IDENTIFIER, fs.IDFEESCHEDULE as FS_ID, fs.IDENTIFIER as FS_IDENTIFIER
from dbo.VW_TRADE_POSETD tr
inner join dbo.VW_ASSET_ETD_EXPANDED aetd on (aetd.IDASSET = tr.IDASSET) {0}
inner join dbo.EVENT e on (e.IDT=tr.IDT)
inner join dbo.EVENTCLASS ec on ec.IDE=e.IDE and ec.EVENTCLASS='VAL' and ec.DTEVENT = tr.DTBUSINESS
inner join dbo.EVENTFEE ef on (ef.IDE=e.IDE) 
inner join dbo.FEESCHEDULE fs on (fs.IDFEESCHEDULE=ef.IDFEESCHEDULE)
where tr.DTBUSINESS=tr.DTENTITY {1} and (tr.IDSTACTIVATION='REGULAR') and (tr.ASSETCATEGORY='ExchangeTradedContract') and (isnull(tr.TRDTYPE,0) not in ({2}))
  and (fs.FEE1FORMULABASIS in ('QuantityContractMultiplier','Premium') or fs.FEE2FORMULABASIS in ('QuantityContractMultiplier','Premium'))
group by tr.IDT, tr.IDENTIFIER, fs.IDFEESCHEDULE, fs.IDENTIFIER", aetdRestrict, dtBusinessRestrict, Cst.TrdType_ExcludedValuesForFees_ETD);

            QueryParameters queryParameters = new QueryParameters(Cs, sqlSelect, dp);

            DataTable dt = DataHelper.ExecuteDataTable(Cs, queryParameters.Query, queryParameters.Parameters.GetArrayDbParameter());
            if (dt.Rows.Count > 0)
            {
                // Liste des enregistrements
                List<DataRow> rows = (from DataRow item in dt.Rows.Cast<DataRow>()
                                      select item
                                       ).ToList();

                // Liste des trades impactés
                var trade = (from DataRow item in rows
                             select new
                             {
                                 id = Convert.ToInt32(item["T_ID"]),
                                 identifier = Convert.ToString(item["T_IDENTIFIER"])
                             }).Distinct();

                List<TradeActionGenMQueue> lstTradeActionGenMQueue = new List<TradeActionGenMQueue>();
                // Pour chaque trade
                foreach (var tradeItem in trade)
                {
                    // Liste des bârèmes
                    var feeschedule = (from DataRow row in rows.Where(x => Convert.ToInt32(x["T_ID"]) == tradeItem.id)
                                       select new
                                       {
                                           id = Convert.ToInt32(row["FS_ID"]),
                                           identifier = Convert.ToString(row["FS_IDENTIFIER"])
                                       }).Distinct();


                    int idt = tradeItem.id;
                    string identifier = tradeItem.identifier;

                    MQueueAttributes mQueueAttributes = new MQueueAttributes()
                    {
                        connectionString = Cs,
                        id = idt,
                        requester = _quotationHandlingMQueue.header.requester,
                        idInfo = new IdInfo()
                        {
                            id = idt,
                            idInfos = new DictionaryEntry[]{new DictionaryEntry("ident", "TRADE"),
                                                                                new DictionaryEntry("identifier", identifier),
                                                                                new DictionaryEntry("GPRODUCT", Cst.ProductGProduct_FUT)}
                        }
                    };

                    // FI 20180328 [23871] FeesCalculationSetting devient FeesCalculationSettingsMode2
                    TradeActionGenMQueue mQueue = new TradeActionGenMQueue(mQueueAttributes)
                    {
                        item = new TradeActionMQueue[]
                        {
                            new TradeActionMQueue
                            {
                                tradeActionCode = TradeActionCode.TradeActionCodeEnum.FeesCalculation,
                                actionMsgs = new FeesCalculationSettingsMode2[1] 
                                {
                                    new FeesCalculationSettingsMode2
                                    {
                                        actionDate  = OTCmlHelper.GetDateSys(Cs),
                                        noteSpecified = true,
                                        note = "Fees Calculation from QuotationHandling Process",
                                        feeSheduleSpecified = true,
                                        feeShedule = (from item in feeschedule
                                                      select new FeeSheduleId() { OTCmlId = item.id, identifier = item.identifier }).ToArray()
                                    }
                                }
                            }
                        }
                    };
                    mQueue.AddParameter(TradeActionGenMQueue.PARAM_ISFORCEDFEES_PRESERVED, true);
                    lstTradeActionGenMQueue.Add(mQueue);
                }

                SendFeesCalculation(lstTradeActionGenMQueue.ToArray());
            }

            return ret;
        }


        /// <summary>
        ///  postage à 
        /// </summary>
        /// <param name="pTradeActionGenMQueue"></param>
        /// FI 20170306 [22225] Add
        private void SendFeesCalculation(TradeActionGenMQueue[] pTradeActionGenMQueue)
        {
            MQueueTaskInfo taskInfo = new MQueueTaskInfo
            {
                connectionString = Cs,
                Session = Session,
                process = pTradeActionGenMQueue[0].ProcessType,
                mQueue = pTradeActionGenMQueue,
                sendInfo = ServiceTools.GetMqueueSendInfo(Cst.ProcessTypeEnum.TRADEACTGEN, AppInstance)
            };


            Tracker.AddPostedSubMsg(ArrFunc.Count(pTradeActionGenMQueue), Session);
            int idTRK_L = MQueue.header.requester.idTRK;
            MQueueTaskInfo.SendMultiple(taskInfo, ref idTRK_L);

            foreach (TradeActionGenMQueue item in pTradeActionGenMQueue)
            {
                string logInfo = LogTools.IdentifierAndId(item.identifier, item.id);
                
                Logger.Log(new LoggerData(LogLevelEnum.Info, new SysMsgCode(SysCodeEnum.LOG, 410), 0,
                    new LogParam(item.id, default, "TRADE", Cst.LoggerParameterLink.IDDATA),
                    new LogParam(logInfo)));

            }
        }
        #endregion Methods
    }
}
