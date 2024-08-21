#region using directives
using System;
using System.Collections;
using System.Collections.Specialized;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using System.Collections.Generic;  
using System.Diagnostics;
using System.Globalization;
using System.Xml.Serialization;
using System.IO;
using System.Threading;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.HtmlControls;
using System.Web;
using System.Drawing;

using System.ServiceModel;
using System.ServiceModel.Description;
using System.ServiceModel.Syndication;
using System.ServiceModel.Web;


using EFS.ACommon;
using EFS.Actor;
using EFS.ApplicationBlocks.Data;
using EFS.Common;
using EFS.Common.Web;
using EFS.Common.MQueue;



using EFS.Permission;
using EFS.Restriction;
using EfsML.DynamicData;

using EfsML;
using EfsML.Business;
using System.Text.RegularExpressions;
using EFS.Syndication;
#endregion using directives

namespace EFS.Referential
{
    #region class SQLReferentialData
    public class SQLReferentialData
    {
        #region Constant/Enum
        private const string sql_GroupByNumber = "GroupByNumber";
        private const string sql_GroupByCount = "GroupByCount";

        /// <summary>
        /// 
        /// </summary>
        public enum SelectedColumnsEnum
        {
            All,
            NoHideOnly,
            None
        }
        #endregion
        //
        #region Constructor(s)
        public SQLReferentialData() { }
        #endregion Constructor(s)
        //
        #region public ApplyChangesInSQLTable
        /// <summary>
        /// Update le dataset passé en arg ainsi que d'eventuelles tables enfants de referential
        /// </summary>
        /// <param name="pSource"></param>
        /// <param name="pDbTransaction"></param>
        /// <param name="pReferential">classe referential</param>
        /// <param name="pDataSet">dataset contenant les données</param>
        /// <param name="opRowsAffected">OUT nb de lignes affectées</param>
        /// <param name="opMessage">OUT message d'erreur (traduit)</param>
        /// <param name="opError">OUT erreur (brute, non traduite)</param>
        /// <param name="pIdMenu"></param>
        /// <returns></returns>
        /// EG 20180423 Analyse du code Correction [CA2200]
        // EG 20220908 [XXXXX][WI418] Suppression de la classe obsolète EFSParameter
        // EG 20220909 [XXXXX][WI418] Correction pb de cast.
        public static Cst.ErrLevel ApplyChangesInSQLTable(string pSource, IDbTransaction pDbTransaction, ReferentialsReferential pReferential, DataSet pDataSet,
            out int opRowsAffected, out string opMessage, out string opError, string pIdMenu)
        {
            Cst.ErrLevel ret = Cst.ErrLevel.SUCCESS;
            opError = string.Empty;
            opMessage = string.Empty;
            opRowsAffected = 0;

            bool isNeedUpdate = false;
            // Checking if datas need to be updated
            foreach (DataTable dt in pDataSet.Tables)
            {
                isNeedUpdate = (null != dt.GetChanges());
                if (isNeedUpdate)
                    break;
            }

            if (isNeedUpdate)
            {
                try
                {
                    SQLReferentialData.SQLSelectParameters sqlSelectParameters = new SQLSelectParameters(pSource, pReferential)
                    {
                        isForExecuteDataAdapter = true
                    };

                    QueryParameters query = GetSQLSelect(sqlSelectParameters, out ArrayList alChildSQLSelect, out ArrayList _, out bool _);
                    string SQLSelect = query.GetQueryReplaceParameters(false);

                    #region Specifique au referentiel virtuel: QUOTE_H_EOD
                    // --------------------------------------------------------------------------------------------------------------------------------------------------------
                    // PL/FI 20110707 Les mises à jour effectuées depuis le référentiel "virtuel" QUOTE_H_EOD 
                    //                sont ensuite déversées dans les tables spécifiques EX QUOTE_ETD_H, QUOTE_EQUITY_H...
                    // --------------------------------------------------------------------------------------------------------------------------------------------------------
                    string referentialTableName = pReferential.TableName;
                    if (referentialTableName == "QUOTE_H_EOD")
                    {
                        //WARNING: Codage en "dur" 
                        //Pour rappel, on est ici sur une vue qui expose virtuellement les cotations nécessaires à une date de compensation donnée.
                        //La donnée valueDataKeyField contient: convert(varchar,isnull(q.IDQUOTE_H,0))+';'+'ETD'+';'+convert(varchar,a.IDASSET)
                        //Par conséquent, quand valueDataKeyField commence par "0;" cela indique une nouvelle cotation (IDQUOTE_H = 0).
                        string[] keyElements = pDataSet.Tables[0].Rows[0].ItemArray[0].ToString().Split(new char[] { ';' });
                        string quoteType = keyElements[1];
                        referentialTableName = "QUOTE_" + quoteType + "_H"; //ex. QUOTE_ETD_H

                        //Rename de la table dans la Query destinée au DataAdapter pour la maj de la table
                        SQLSelect = SQLSelect.Replace(SQLCst.DBO + "QUOTE_H_EOD", SQLCst.DBO + referentialTableName);

                        //Suppression de la colonne fictive dans la Query destinée au DataAdapter pour la maj de la table
                        SQLSelect = SQLSelect.Replace(SQLCst.TBLMAIN + ".KEYVALUE" + SQLCst.AS + "KEYVALUE,", string.Empty);
                        SQLSelect = SQLSelect.Replace(SQLCst.TBLMAIN + ".ASSET_IDENTIFIER" + SQLCst.AS + "ASSET_IDENTIFIER,", string.Empty);
                        SQLSelect = SQLSelect.Replace(SQLCst.TBLMAIN + ".ASSET_TYPE" + SQLCst.AS + "ASSET_TYPE,", string.Empty);
                        SQLSelect = SQLSelect.Replace(SQLCst.TBLMAIN + ".ASSET_IDM" + SQLCst.AS + "ASSET_IDM,", string.Empty);
                        SQLSelect = SQLSelect.Replace(SQLCst.TBLMAIN + ".ASSET_IDDC" + SQLCst.AS + "ASSET_IDDC,", string.Empty);
                        SQLSelect = SQLSelect.Replace(SQLCst.TBLMAIN + ".ASSET_IDMATURITY" + SQLCst.AS + "ASSET_IDMATURITY,", string.Empty);
                        SQLSelect = SQLSelect.Replace(SQLCst.TBLMAIN + ".ASSET_CATEGORY" + SQLCst.AS + "ASSET_CATEGORY,", string.Empty);
                        SQLSelect = SQLSelect.Replace(SQLCst.TBLMAIN + ".ASSET_STRIKEPRICE" + SQLCst.AS + "ASSET_STRIKEPRICE,", string.Empty);
                        SQLSelect = SQLSelect.Replace(SQLCst.TBLMAIN + ".ASSET_TICKVALUE" + SQLCst.AS + "ASSET_TICKVALUE,", string.Empty);
                        SQLSelect = SQLSelect.Replace(SQLCst.TBLMAIN + ".ASSET_TICKSIZE" + SQLCst.AS + "ASSET_TICKSIZE,", string.Empty);
                        SQLSelect = SQLSelect.Replace(SQLCst.TBLMAIN + ".ASSET_MULTIPLIER" + SQLCst.AS + "ASSET_MULTIPLIER,", string.Empty);
                        SQLSelect = SQLSelect.Replace(SQLCst.TBLMAIN + ".ASSET_DEN" + SQLCst.AS + "ASSET_DEN,", string.Empty);
                        SQLSelect = SQLSelect.Replace(SQLCst.TBLMAIN + ".ASSET_MATFMT_PROFIL" + SQLCst.AS + "ASSET_MATFMT_PROFIL,", string.Empty);
                        SQLSelect = SQLSelect.Replace(SQLCst.TBLMAIN + ".ASSET_DC_IDENT" + SQLCst.AS + "ASSET_DC_IDENT,", string.Empty);
                        SQLSelect = SQLSelect.Replace(SQLCst.TBLMAIN + ".ASSET_DC_DSP" + SQLCst.AS + "ASSET_DC_DSP,", string.Empty);
                        SQLSelect = SQLSelect.Replace(SQLCst.TBLMAIN + ".ASSET_PUTCALL" + SQLCst.AS + "ASSET_PUTCALL,", string.Empty);

                        //Suppression des colonnes spécifiques aux ETD 
                        if (quoteType != "ETD")
                        {
                            SQLSelect = SQLSelect.Replace(SQLCst.TBLMAIN + ".TIMETOEXPIRATION" + SQLCst.AS + "TIMETOEXPIRATION,", string.Empty);
                            SQLSelect = SQLSelect.Replace(SQLCst.TBLMAIN + ".CONTRACTMULTIPLIER" + SQLCst.AS + "CONTRACTMULTIPLIER,", string.Empty);
                        }
                    }
                    #endregion Specifique au referentiel virtuel: QUOTE_H_EOD

                    //Initialisation d'un objet mQueueDataset dans le cas où les données du référentiel nécessitent le postage d'un message à un service business.
                    MQueueDataset mQueueDataset = new MQueueDataset(pSource, referentialTableName);
                    if (mQueueDataset.IsAvailable)
                        mQueueDataset.Prepare(pDataSet.Tables[0]);
                    else
                        mQueueDataset = null;

                    int nRows = 0;
                    if (pDbTransaction == null)
                    {
                        nRows = DataHelper.ExecuteDataAdapter(pSource, SQLSelect, pDataSet.Tables[0]);
                    }
                    else
                    {
                        nRows = DataHelper.ExecuteDataAdapter(pDbTransaction, SQLSelect, pDataSet.Tables[0]);
                    }
                    if (nRows < 0)
                    {
                        ret = Cst.ErrLevel.FAILURE;
                    }
                    else if (Cst.ErrLevel.SUCCESS == ret)
                    {
                        #region Externals datas
                        if (pReferential.HasMultiTable)
                        {
                            if (!pReferential.isNewRecord)
                            {
                                //if not new record, updating externals datas if exists
                                for (int i = 1; i < pDataSet.Tables.Count; i++)
                                {
                                    if (pDataSet.Tables[i].Rows.Count > 0)
                                    {
                                        SQLSelect = alChildSQLSelect[i - 1].ToString();
                                        if (pDbTransaction == null)
                                        {
                                            nRows += DataHelper.ExecuteDataAdapter(pSource, SQLSelect, pDataSet.Tables[i]);
                                        }
                                        else
                                        {
                                            nRows += DataHelper.ExecuteDataAdapter(pDbTransaction, SQLSelect, pDataSet.Tables[i]);
                                        }
                                    }
                                }
                            }
                            else if (pReferential.IsForm && (pReferential.IndexKeyField != -1))
                            {
                                //if new record and exists Keyfield: get the new created ID with keyField for the new externals datas
                                string sqlSelect = string.Empty;
                                sqlSelect += SQLCst.SELECT + pReferential.Column[pReferential.IndexDataKeyField].ColumnName + " as OVALUE";
                                sqlSelect += SQLCst.FROM_DBO + referentialTableName;
                                sqlSelect += SQLCst.WHERE + pReferential.Column[pReferential.IndexKeyField].ColumnName + "=" + DataHelper.GetVarPrefix(pSource) + "PARAM";

                                DataParameter sqlParam;
                                //formating data with datatype of keyfield
                                if (TypeData.IsTypeInt(pReferential.Column[pReferential.IndexKeyField].DataType.value))
                                    sqlParam = new DataParameter(pSource, "PARAM", DbType.Int64);
                                else if (TypeData.IsTypeBool(pReferential.Column[pReferential.IndexKeyField].DataType.value))
                                    sqlParam = new DataParameter(pSource, "PARAM", DbType.Boolean);
                                else if (TypeData.IsTypeDec(pReferential.Column[pReferential.IndexKeyField].DataType.value))
                                    sqlParam = new DataParameter(pSource, "PARAM", DbType.Decimal);
                                else if (TypeData.IsTypeDate(pReferential.Column[pReferential.IndexKeyField].DataType.value))  // FI 20201006 [XXXXX] DbType.Date sur une donnée de type Date
                                    sqlParam = new DataParameter(pSource, "PARAM", DbType.Date);
                                else if (TypeData.IsTypeDateTime(pReferential.Column[pReferential.IndexKeyField].DataType.value))
                                    sqlParam = new DataParameter(pSource, "PARAM", DbType.DateTime);
                                else if (TypeData.IsTypeDateTimeOffset(pReferential.Column[pReferential.IndexKeyField].DataType.value))
                                    sqlParam = new DataParameter(pSource, "PARAM", DbType.DateTimeOffset);
                                else
                                    sqlParam = new DataParameter(pSource, "PARAM", DbType.AnsiString, 64);

                                DataParameters parameters = new DataParameters();
                                parameters.Add(sqlParam, pReferential.dataRow[pReferential.IndexColSQL_KeyField]);

                                object oValue = DataHelper.ExecuteScalar(pSource, pDbTransaction, CommandType.Text, sqlSelect, parameters.GetArrayDbParameter());
                                if (oValue != null)
                                {
                                    //affecting ID to each externals datas
                                    for (int i = 0; i < pReferential.drExternal.GetLength(0); i++)
                                    {
                                        if (pReferential.isNewDrExternal[i])
                                        {
                                            pReferential.drExternal[i].BeginEdit();
                                            pReferential.drExternal[i]["ID"] = oValue;
                                            pReferential.drExternal[i].EndEdit();
                                        }
                                    }
                                    //updating each externals datas
                                    for (int i = 0; i < alChildSQLSelect.Count; i++)
                                    {
                                        if (pDataSet.Tables[i + 1].Rows.Count > 0)
                                        {
                                            SQLSelect = alChildSQLSelect[i].ToString();
                                            if (pDbTransaction == null)
                                            {
                                                nRows += DataHelper.ExecuteDataAdapter(pSource, SQLSelect, pDataSet.Tables[i + 1]);
                                            }
                                            else
                                            {
                                                nRows += DataHelper.ExecuteDataAdapter(pDbTransaction, SQLSelect, pDataSet.Tables[i + 1]);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        #endregion Externals datas
                    }

                    
                    if ((Cst.ErrLevel.SUCCESS == ret) && (mQueueDataset != null))
                    {
                        mQueueDataset.Send(pDbTransaction, SessionTools.AppSession,  pIdMenu);
                    }
                    

                    opRowsAffected = nRows;
                }
                catch (Exception e)
                {
                    // if an exception occurs; analyse it with AnalyseSQLException()
                    opRowsAffected = 0;
                    bool isSQLException = DataHelper.AnalyseSQLException(pSource, e, out opMessage, out opError);
                    if (isSQLException)
                        ret = Cst.ErrLevel.SQLDEFINED;//an SQL error occurs
                    else
                        throw;
                }
                finally
                {
                    //Suppression de la derniere ligne si error SQL
                    if (ret == Cst.ErrLevel.SQLDEFINED && (pReferential.isNewRecord))
                    {
                        pReferential.dataSet.Tables[0].Rows.RemoveAt(pReferential.dataSet.Tables[0].Rows.Count - 1);
                    }
                }
            }

            return ret;
        }
        #endregion ApplyChangesInSQLTable
        #region public DeleteDataInSQLTable
        /// <summary>
        /// Delete la ligne correspondant au DataKeyField passé en arg pour le referential
        /// </summary>
        /// <param name="pReferential">classe referential</param>
        /// <param name="pDataKeyValue">valeur de DataKeyField</param>
        /// <returns>Cst.ErrLevel</returns>
        public static Cst.ErrLevel DeleteDataInSQLTable(string pSource, ReferentialsReferential pReferential, string pDataKeyValue)
        {
            Cst.ErrLevel ret = Cst.ErrLevel.ABORTED;
            //
            //On execute le delete sur la table main
            string SQLDelete = GetSQLDelete(pReferential, pDataKeyValue);
            int rowsaffected = DataHelper.ExecuteNonQuery(pSource, CommandType.Text, SQLDelete);
            //
            //Si des enregistrements sont présents dans EXTLID pour cet enregistrement, on les delete aussi
            SQLDelete = GetSQLDeleteExternals(pReferential, pDataKeyValue);
            rowsaffected += DataHelper.ExecuteNonQuery(pSource, CommandType.Text, SQLDelete);
            //
            if (rowsaffected > 0)
                ret = Cst.ErrLevel.SUCCESS;
            //
            return ret;

        }
        #endregion DeleteDataInSQLTable
        #region public RetrieveDataFromSQLTable
        /// <revision>
        ///     <version>2.2.0</version><date>20090109</date><author>PL</author>
        ///     <comment>
        ///     New parameter: pIsFirstTableOnly
        ///     </comment>
        /// </revision>	
        /// <summary>
        /// Génére un DataSet contenant:
        /// - une table principale (table[0]) 
        /// - 0 à N tables secondaires, utilisées pour ajouter/modifier des données se trouvant dans d'autres tables que la tables principale (ex. EXTLID)
        /// </summary>
        /// <param name="pReferential">classe referential</param>
        /// <param name="pColumnFK">colonne pour un where specifique</param>
        /// <param name="pValueFKFormated">valeur de la colonne pour un where specifique</param>
        /// <param name="pForeignKeyValue">
        /// additional value, filled with the foreign key value 
        /// when the pColumnValue does not contain the foreign key value, but the data key instead</param>

        public static DataSet RetrieveDataFromSQLTable(string pCS, ReferentialsReferential pReferential, string pColumn, string pColumnValue, bool pIsColumnDataKeyField,
                                                        bool pIsTableMainOnly, string pForeignKeyValue)
        {

            SQLReferentialData.SQLSelectParameters sqlSelectParameters = new SQLReferentialData.SQLSelectParameters(pCS, SelectedColumnsEnum.All, pReferential);
            QueryParameters query = GetQuery_LoadReferential(sqlSelectParameters, pColumn, pColumnValue, pIsColumnDataKeyField,
                                            pIsTableMainOnly, out _, out ArrayList alChildTablename, out _);

            // FI 20160916 [22471] Modify (add variables indexForeignKeyField, dataTypeForeignKeyField) => facilite juste la lecture du code
            int indexForeignKeyField = pReferential.IndexForeignKeyField;
            string dataTypeForeignKeyField = indexForeignKeyField > 0 ? pReferential[pReferential.IndexForeignKeyField].DataType.value : null;


            // 20120313 MF Evaluate Dynamic argument relative to foreign key %%FOREIGN_KEY%%
            query.Query = EvaluateForeignKeyDynamicArgument(query.Query, indexForeignKeyField, dataTypeForeignKeyField,
                pIsColumnDataKeyField, pColumnValue, pForeignKeyValue);

            //PL 20110303 TEST for SQLServer WITH (TBD)
            query.Query = TransformRecursiveQuery(pCS, pReferential, pColumn, pColumnValue, pIsColumnDataKeyField, query.Query);
            DataSet ret = DataHelper.ExecuteDataset(pCS, CommandType.Text, query.Query, query.Parameters.GetArrayDbParameter());

            //Le DataSet récupéré contient N tables (autant que de queries), on leur affecte leur TableName pour les identifier
            ret.Tables[0].TableName = pReferential.TableName;
            for (int i = 1; i < ret.Tables.Count; i++)
                ret.Tables[i].TableName = alChildTablename[i - 1].ToString();

            return ret;
        }

        public static DataSet RetrieveDataFromSQLTable(string pCS, ReferentialsReferential pReferential, string pColumn, string pColumnValue, bool pIsColumnDataKeyField, bool pIsTableMainOnly)
        {
            return RetrieveDataFromSQLTable(pCS, pReferential, pColumn, pColumnValue, pIsColumnDataKeyField, pIsTableMainOnly, String.Empty);
        }

        #endregion RetrieveDataFromSQLTable

        /// <summary>
        /// Replace the Foreign Key dynamic argument (%%FOREIGN_KEY%%) with the value passed inside the input argument pColumnValue
        /// </summary>
        /// <param name="pQuery">query where we want to perform the replacement</param>
        /// <param name="pIndexForeignKey">index of the Referential field where the foreign key has been found. IsForeignKeyField:=true</param>
        /// <param name="pDataTypeForeignKeyField">data type of the Referential field where the foreign key has been found</param>
        /// <param name="pIsColumnValueRelativeToDataKey">Check for pColumnValue parameter, when true then the value of pColumnValue is
        /// NOT bound to the foreign key but to the data key</param>
        /// <param name="pColumnValue">replacement value for the dynamic argument</param>
        /// <param name="pForeignKeyValue">
        /// additional value, filled with the foreign key value 
        /// when the pColumnValue does not contain the foreign key value, but the data key instead</param>
        /// <returns>the input string with the evaluated dynamic argument or in the original state </returns>
        /// <exception cref="NotSupportedException">in any case we need to use the pForeignKeyValue and this one is not well initialized </exception>
        public static string EvaluateForeignKeyDynamicArgument(string pQuery, int pIndexForeignKey, string pDataTypeForeignKeyField,
            bool pIsColumnValueRelativeToDataKey, string pColumnValue, string pForeignKeyValue)
        {
            string res = pQuery;

            TypeData.TypeDataEnum typeData = TypeData.TypeDataEnum.unknown;

            if (!String.IsNullOrEmpty(pDataTypeForeignKeyField))
            {
                typeData = TypeData.GetTypeDataEnum(pDataTypeForeignKeyField, false);
            }

            // 1. Check arguments, when the foreign key is not declared OR 
            //  the data type of the foreign key is missing, then the query is returned without any replacements
            if (pIndexForeignKey < 0 || typeData == TypeData.TypeDataEnum.unknown || !pQuery.Contains(Cst.FOREIGN_KEY))
            {
                return res;
            }

            // 1.1 In case the replacement may be performed and we need to use the pForeignKeyValue argument, but the argument is not
            //   well initialized an exception is raised
            if (pIsColumnValueRelativeToDataKey && String.IsNullOrEmpty(pForeignKeyValue))
            {
                throw new ArgumentException(@"The foreign key value is not well initialised 
                    the %%FOREIGN_KEY%% dynamic argument can not be evaluated.", "pForeignKeyValue");
            }

            // 2. check if columnValue is related to the foreign key, else replace that with the explicit foreign key value
            if (pIsColumnValueRelativeToDataKey)
            {
                pColumnValue = pForeignKeyValue;
            }

            // 3. optional value formatting, and special cases for  empty pColumnValue
            switch (typeData)
            {
                case TypeData.TypeDataEnum.@string:

                    if (String.IsNullOrEmpty(pColumnValue))
                    {
                        pColumnValue = Cst.NotAvailable;
                    }

                    pColumnValue = DataHelper.SQLString(pColumnValue);

                    break;

                default:

                    if (String.IsNullOrEmpty(pColumnValue))
                    {
                        pColumnValue = "-1";
                    }

                    break;
            }

            // 4. replacement
            res = pQuery.Replace(Cst.FOREIGN_KEY, pColumnValue);

            return res;
        }

        //
        #region private GetSQLDelete
        /// <summary>
        /// Renvoie la requete SQL pour le delete d'un ligne dont on passe la valeur du dataKeyField en arg
        /// </summary>
        /// <param name="pReferential">classe referential</param>
        /// <param name="pDataKeyValue">valeur du DataKeyfield a supprimer</param>
        /// <returns>requete DELETE</returns>
        private static string GetSQLDelete(ReferentialsReferential pReferential, string pDataKeyValue)
        {
            string SQLQuery = string.Empty;
            if (pReferential.IndexDataKeyField != -1)
            {
                SQLQuery = SQLCst.DELETE_DBO + pReferential.TableName + Cst.CrLf;
                SQLQuery += SQLCst.WHERE;
                ReferentialsReferentialColumn rrc = pReferential.Column[pReferential.IndexDataKeyField];
                if (TypeData.IsTypeString(rrc.DataType.value))
                    pDataKeyValue = DataHelper.SQLString(pDataKeyValue);
                SQLQuery += pReferential.Column[pReferential.IndexDataKeyField].ColumnName + "=" + pDataKeyValue;
            }
            return SQLQuery;
        }
        #endregion GetSQLDelete
        #region private GetSQLDeleteExternals
        /// <summary>
        /// Renvoie la requete SQL pour le delete des lignes de type externes (EXTLID par exemple)
        /// dont on passe la valeur du dataKeyField en arg
        /// </summary>
        /// <param name="pReferential">classe referential</param>
        /// <param name="pDataKeyValue">valeur du DataKeyfield a supprimer</param>
        /// <returns>requete DELETE</returns>
        private static string GetSQLDeleteExternals(ReferentialsReferential pReferential, string pDataKeyValue)
        {
            string SQLQuery = SQLCst.DELETE_DBO + Cst.OTCml_TBL.EXTLID.ToString() + Cst.CrLf;
            SQLQuery += SQLCst.WHERE + "ID=";
            if (pReferential.IsDataKeyField_String)
                SQLQuery += DataHelper.SQLString(pDataKeyValue);
            else
                SQLQuery += pDataKeyValue;
            SQLQuery += SQLCst.AND + "TABLENAME=" + DataHelper.SQLString(pReferential.TableName);
            //
            return SQLQuery;
        }
        #endregion GetSQLDeleteExternals
        //
        #region public GetSQLSelect
        /// <param name="pReferential">Classe referential</param>
        /// <param name="pSQLWhere">Where à implementer si existant</param>
        /// <param name="pIsForExecuteDataAdapter">Requete destinée à être passée à un pIsForExecuteDataAdapter (donc requête uniquement sur la table ppale</param>
        /// <param name="opDataTablesSQLChild">OUT requetes SQL des datatables enfants</param>
        /// <param name="opDataTablesChild">OUT datatables des tables enfants</param>

        /// <summary>
        /// 
        /// </summary>
        /// <param name="pSQLSelectParameters"></param>
        /// <param name="opAlChildSQLselect"></param>
        /// <param name="opAlChildTableName"></param>
        /// <param name="isQueryWithSubTotal">OUT si la requête contient un Group by</param>
        /// <returns></returns>
        public static QueryParameters GetSQLSelect(SQLSelectParameters pSQLSelectParameters,
                                                    out ArrayList opAlChildSQLselect, out ArrayList opAlChildTableName, out bool isQueryWithSubTotal)
        {
            GetSQL(pSQLSelectParameters, out opAlChildSQLselect, out opAlChildTableName, out QueryParameters sqlQuery, out _, out _, out _, out isQueryWithSubTotal);
            return sqlQuery;
        }
        public static QueryParameters GetSQLSelect(SQLSelectParameters pSQLSelectParameters)
        {
            GetSQL(pSQLSelectParameters, out _, out _, out QueryParameters sqlQuery, out _, out _, out _, out _);
            return sqlQuery;
        }
        #endregion
        #region public GetSQLCriteria
        public static SQL_Criteria GetSQLCriteria(string pSource, ReferentialsReferential pReferential)
        {
            GetSQL(pSource, SelectedColumnsEnum.All, pReferential, null,
                    false, false, null, out _, out _, out _, out _, out SQL_Criteria sql_Criteria, out _, out _);
            return sql_Criteria;
        }
        #endregion
        #region public GetSQLWhere
        public static string GetSQLWhere(string pSource, ReferentialsReferential pReferential, string pSQLWhere)
        {
            GetSQL(pSource, SelectedColumnsEnum.All, pReferential, pSQLWhere,
                    false, false, null, out _, out _, out _, out string sqlWhere, out _, out _, out _);

            return sqlWhere;
        }
        #endregion
        #region public GetSQLOrderBy
        public static string GetSQLOrderBy(string pSource, ReferentialsReferential pReferential)
        {
            return GetSQLOrderBy(pSource, pReferential, false, false);
        }
        public static string GetSQLOrderBy(string pSource, ReferentialsReferential pReferential, bool pIsUseColumnAlias, bool pIsUseColumnAliasInOrderExpression)
        {
            string sqlHint = "~"; //PL 20180626 New "~" ( cf. GetSQL() )

            GetSQL(pSource, SelectedColumnsEnum.All, pReferential, string.Empty,
                false, false, pIsUseColumnAlias, pIsUseColumnAliasInOrderExpression, sqlHint,
                out _, out _, out _, out _, out _, out string sqlOrderBy, out _);

            return sqlOrderBy;
        }
        #endregion

        #region public GetQuery_LoadReferential
        public static QueryParameters GetQuery_LoadReferential(SQLSelectParameters pSQLSelectParameters,
                                            string pColumn, string pColumnValue, bool pIsColumnDataKeyField,
                                            bool pIsTableMainOnly,
                                            out ArrayList alChildSQLSelect, out ArrayList alChildTablename, out bool isQueryWithSubTotal)
        {
            string sqlWhere = BuildSqlWhere(pSQLSelectParameters.referential, pColumn, pColumnValue, pIsColumnDataKeyField);

            //On récupère la query de la table ppale, ainsi que d'événtuelles queries child
            pSQLSelectParameters.sqlWhere = sqlWhere;
            QueryParameters ret = GetSQLSelect(pSQLSelectParameters, out alChildSQLSelect, out alChildTablename, out isQueryWithSubTotal);

            // EG 202201102 Remplacement des %%WHEREDATAKEY%% par la clause Where de base : sqlWhere
            // EG 20240125 [WI825] Logs: Harmonization data of consultation(via Tracker or Processing requests)
            if (ret.Query.Contains(Cst.WHEREDATAKEY))
                ret.Query = ret.Query.Replace(Cst.WHEREDATAKEY, StrFunc.IsFilled(sqlWhere)?sqlWhere:"0=1");

            if ((!pIsTableMainOnly) && pSQLSelectParameters.referential.HasMultiTable)
            {
                //On ajoute les queries pour les tables secondaires 
                for (int i = 0; i < alChildSQLSelect.Count; i++)
                {
                    ret.Query += SQLCst.SEPARATOR_MULTISELECT;
                    ret.Query += alChildSQLSelect[i].ToString();

                    if (StrFunc.IsFilled(sqlWhere))
                    {
                        //PL 20111227 Suite à pb de taille sous Oracle
                        //if (alChildTablename[i].ToString().StartsWith("ex_"))
                        if (alChildTablename[i].ToString().StartsWith("e_"))
                        {
                            //"e_" --> Table EXTLID (ou EXTLIDS), donc la colonne "key" est tjs "ID"
                            if (sqlWhere.IndexOf("'NewRecord'") > 0) //Tip
                            {
                                ret.Query += SQLCst.AND + sqlWhere;
                            }
                            else
                            {
                                ret.Query += SQLCst.AND + "ID=" + BuildValueFormated(pSQLSelectParameters.referential, pColumnValue, pIsColumnDataKeyField);
                            }
                        }
                        else
                        {
                            //Autres tables (ex. ACTORROLE)
                            //EG 20110308
                            if (sqlWhere.IndexOf("'NewRecord'") > 0) //Tip
                            {
                                ret.Query += SQLCst.AND + sqlWhere;
                            }
                            else
                            {
                                //PL 20170913 [23409] Newness (Tips...)
                                //                    Ici, il n'est pas possible de savoir si on est sur une table relative à un élément <Items>
                                //                    Par conséquent, on fait au mieux... On regarde s'il existe un élément <Items> avec un attribut "datacolumnname"
                                //                    et si tel est le cas on regarde si la query comporte bien dans sa liste de colonnes, 
                                //                    une colonne, d'alias ID, de même nom que celui de la colonne PK de la table principale. 
                                //                    Si tel n'est pas le cas on remplace dans le where le nom de la colonne PK par celui spécifié dans l'attribut <Items datacolumnname="xxx">
                                //                    ex. CONTRACTG --> "IDXC" instead of "IDDC" and "IDCC"
                                string tmp_sqlWhere = sqlWhere;
                                if (pSQLSelectParameters.referential.ItemsSpecified && pSQLSelectParameters.referential.Items.datacolumnnameSpecified)
                                {
                                    if (alChildSQLSelect[i].ToString().IndexOf(pColumn + " as ID") == -1)
                                    {
                                        tmp_sqlWhere = sqlWhere.Replace(pColumn, pSQLSelectParameters.referential.Items.datacolumnname);
                                    }
                                }

                                ret.Query += SQLCst.AND + SetAliasToSQLWhere(alChildTablename[i].ToString(), tmp_sqlWhere);
                            }
                        }
                    }
                }
                ret.Query += SQLCst.SEPARATOR_MULTISELECT;
            }

            return ret;
        }
        #endregion
        //
        #region public GetQueryCountReferential
        /// <summary>
        /// Retourne la query qui permet de déterminer le nombre de lignes d'un referentiel 
        /// <remarks>
        /// <para>
        /// - Format d'une query Referential: select ... from ...  order by ...
        /// </para>
        /// <para>
        /// - Format d'une query Count Referential: select 1 from ...
        /// </para>
        /// </remarks> 
        /// </summary>
        /// <param name="pSQLSelectParameters"></param>
        /// <param name="pColumn"></param>
        /// <param name="pColumnValue"></param>
        /// <param name="pIsColumnDataKeyField"></param>
        /// <returns></returns>
        // EG 20200720 [XXXXX] Nouvelle interface GUI v10 (Mode Noir ou blanc)
        public static QueryParameters GetQueryCountReferential(SQLSelectParameters pSQLSelectParameters, string pColumn, string pColumnValue)
        {
            bool isTableMainOnly_True = true;
            bool isColumnDataKeyField_False = false;
            QueryParameters queryLoad = GetQuery_LoadReferential(pSQLSelectParameters, pColumn, pColumnValue, isColumnDataKeyField_False,
                                                                 isTableMainOnly_True, out _, out _, out _);

            if ((pSQLSelectParameters.selectedColumns == SelectedColumnsEnum.All)
                || (pSQLSelectParameters.referential.CmptLevelSpecified && pSQLSelectParameters.referential.CmptLevel == "2.5"))
            {
                //Compatibilité avec le principe en vigueur en v2.5, principe encore utilisé pour les consultations LST où il est impossible actuellement d'identifier les jointures remontant plus d'une ligne. 
                string queryOrder = GetSQLOrderBy(pSQLSelectParameters.source, pSQLSelectParameters.referential);
                if (StrFunc.IsFilled(queryOrder))
                    queryLoad.Query = queryLoad.Query.Replace(queryOrder, string.Empty);

                queryLoad.Query = SQLCst.SELECT + SQLCst.COUNT_1 + Cst.CrLf + SQLCst.X_FROM + "(" + queryLoad.Query + ") tblGetQueryCountReferential";
            }

            return queryLoad;
        }
        #endregion
        #region public GetColumnSortInGroupBy
        // RD 20091102 / Utilisation de sqlColumn            
        public static string GetColumnSortInGroupBy(SQLReferentialColumn pSqlColumn,
            ref string pSql_SelectSort, ref string pSql_SelectGBSort)
        {
            // RD 20091214 / 16802/ LSTCOLUMN.SQLSELECT
            return GetColumnSortInGroupBy(pSqlColumn.SQLColumnNameOrSQLColumnSQLSelect, pSqlColumn.SqlColumnAlias,
            ref pSql_SelectSort, ref pSql_SelectGBSort);
        }
        public static string GetColumnSortInGroupBy(string pSqlColumnName, string pSqlColumnAlias,
            ref string pSql_SelectSort, ref string pSql_SelectGBSort)
        {
            //PLl 20101102 "_GroupBySort" --> "_GS" (pour éviter les alias > 30 car. sous Oracle)
            //sqlGroupBySort = pSqlColumnAlias + "_GroupBySort";
            string sqlGroupBySort = pSqlColumnAlias + "_GS";

            string selectSort = SQLCst.CASE + SQLCst.CASE_WHEN + " {0} " + SQLCst.IS_NULL;
            selectSort += SQLCst.CASE_THEN + "1" + SQLCst.CASE_ELSE + "0" + SQLCst.CASE_END;
            selectSort += SQLCst.AS + sqlGroupBySort + "," + Cst.CrLf;

            pSql_SelectSort += StrFunc.AppendFormat(selectSort, pSqlColumnName);
            pSql_SelectGBSort += StrFunc.AppendFormat(selectSort, pSqlColumnAlias);

            sqlGroupBySort += ",";

            return sqlGroupBySort;
        }
        #endregion GetColumnSortInGroupBy
        // RD 20091216 / 16802/ LSTCOLUMN.SQLSELECT
        #region public GetColumnNameOrColumnSelect
        public static string GetColumnNameOrColumnSelect(ReferentialsReferential pReferential, ReferentialsReferentialSQLWhere pSQLWhere)
        {
            string tempAlias = (pSQLWhere.AliasTableNameSpecified ? pSQLWhere.AliasTableName : string.Empty);
            //
            return GetColumnNameOrColumnSelect(pReferential, pSQLWhere.ColumnName, tempAlias, pSQLWhere.ColumnNameOrColumnSQLSelectSpecified, pSQLWhere.ColumnNameOrColumnSQLSelect);
        }
        public static string GetColumnNameOrColumnSelect(ReferentialsReferentialSQLOrderBy pSQLOrderBy)
        {
            ReferentialsReferentialColumn rrc = null;
            return GetColumnNameOrColumnSelect(rrc, pSQLOrderBy.ColumnName, pSQLOrderBy.Alias, pSQLOrderBy.ColumnNameOrColumnSQLSelectSpecified, pSQLOrderBy.ColumnNameOrColumnSQLSelect);
        }
        public static string GetColumnNameOrColumnSelect(ReferentialsReferential pReferential, ReferentialsReferentialSQLOrderBy pSQLOrderBy)
        {
            return GetColumnNameOrColumnSelect(pReferential, pSQLOrderBy.ColumnName, pSQLOrderBy.Alias, pSQLOrderBy.ColumnNameOrColumnSQLSelectSpecified, pSQLOrderBy.ColumnNameOrColumnSQLSelect);
        }
        public static string GetColumnNameOrColumnSelect(ReferentialsReferential pReferential, string pColumnName, string pAliasTableName, bool pColumnNameOrColumnSQLSelectSpecified, string pColumnNameOrColumnSQLSelect)
        {
            ReferentialsReferentialColumn rrc = pReferential[pColumnName, pAliasTableName];
            return GetColumnNameOrColumnSelect(rrc, pColumnName, pAliasTableName, pColumnNameOrColumnSQLSelectSpecified, pColumnNameOrColumnSQLSelect);
        }
        public static string GetColumnNameOrColumnSelect(string pColumnName, string pAliasTableName, bool pColumnNameOrColumnSQLSelectSpecified, string pColumnNameOrColumnSQLSelect)
        {
            ReferentialsReferentialColumn rrc = null;
            return GetColumnNameOrColumnSelect(rrc, pColumnName, pAliasTableName, pColumnNameOrColumnSQLSelectSpecified, pColumnNameOrColumnSQLSelect);
        }

        public static string GetColumnNameOrColumnSelect(ReferentialsReferentialColumn pRrc, string pColumnName, string pAliasTableName, bool pColumnNameOrColumnSQLSelectSpecified, string pColumnNameOrColumnSQLSelect)
        {
            string ret = string.Empty;
            //
            if (null != pRrc && pRrc.ColumnNameOrColumnSQLSelectSpecified)
                ret = pRrc.ColumnNameOrColumnSQLSelect;
            else if (pColumnNameOrColumnSQLSelectSpecified)
                ret = pColumnNameOrColumnSQLSelect;
            //
            if (StrFunc.IsEmpty(ret))
                ret = GetColumnNameExpression(pColumnName, pAliasTableName);
            //
            return ret;
        }

        /// <summary>
        /// <para>Retourne "{pColumnName}" si {pColumnName} contient déjà "." </para>
        /// <para>Retourne "{pAliasTableName}.{pColumnName}" sinon </para>
        /// </summary>
        /// <param name="pColumnName"></param>
        /// <param name="pAliasTableName"></param>
        /// <returns></returns>
        public static string GetColumnNameExpression(string pColumnName, string pAliasTableName)
        {
            string ret = pColumnName;
            if (StrFunc.IsFilled(pAliasTableName) && pColumnName.IndexOf(".") == -1)
                ret = pAliasTableName + "." + ret;
            //
            return ret;
        }
        #endregion GetColumnNameOrColumnSelect
        //
        // RD 20091102 
        // SetGroupBy() renommée en GetSqlGroupBy() 
        // et déplacée dans la nouvelle classe EFS.Referentiel.SQLReferentialColumn
        //
        #region private GetSQL
        /// <revision>
        ///     <version>3.1.0</version><date>20130102</date><author>PL</author>
        ///     <comment>
        ///     New surcharge with parameter SQLSelectParameters 
        ///     </comment>
        /// </revision>
        /// <revision>
        ///     <version>2.3.0</version><date>20090901</date><author>RD</author>
        ///     <comment>
        ///     1- New surcharge with parameter pIsWithColumnAlias
        ///     2- Manage Totals and sub-totals
        ///     </comment>
        /// </revision>
        /// <revision>
        ///     <version>2.3.0</version><date>20091102</date><author>RD</author>
        ///     <comment>
        ///     Utilisation d'un Objet SQLReferentialColumn
        ///     </comment>
        /// </revision>
        private static void GetSQL(SQLSelectParameters pSQLSelectParameters,
            out ArrayList opAlChildSQLselect, out ArrayList opAlChildTableName,
            out QueryParameters opSQLQuery, out string opSQLWhere, out SQL_Criteria opSQL_Criteria, out string opSQLOrderBy, out bool isQueryWithSubTotal)
        {
            GetSQL(pSQLSelectParameters.source, pSQLSelectParameters.selectedColumns, pSQLSelectParameters.referential, pSQLSelectParameters.sqlWhere,
                    pSQLSelectParameters.isForExecuteDataAdapter, pSQLSelectParameters.isSelectDistinct,
                    false, false,
                    pSQLSelectParameters.sqlHints,
                    out opAlChildSQLselect, out opAlChildTableName, out opSQLQuery, out opSQLWhere, out opSQL_Criteria, out opSQLOrderBy, out isQueryWithSubTotal);
        }
        private static void GetSQL(string pSource, SelectedColumnsEnum pSelectedColumns, ReferentialsReferential pReferential, string pSQLWhere,
            bool pIsForExecuteDataAdapter, bool pIsSelectDistinct, string pSQLHints,
            out ArrayList opAlChildSQLselect, out ArrayList opAlChildTableName,
            out QueryParameters opSQLQuery, out string opSQLWhere, out SQL_Criteria opSQL_Criteria, out string opSQLOrderBy, out bool isQueryWithSubTotal)
        {
            GetSQL(pSource, pSelectedColumns, pReferential, pSQLWhere,
                pIsForExecuteDataAdapter, pIsSelectDistinct,
                false, false,
                pSQLHints,
                out opAlChildSQLselect, out opAlChildTableName, out opSQLQuery, out opSQLWhere, out opSQL_Criteria, out opSQLOrderBy, out isQueryWithSubTotal);
        }
        // EG 20110906 Add SQLCheckSelectedDefaultValueSpecified/SQLCheckSelectedDefaultValue
        // EG 20141020 [20442] Add GContractRole for DC to Invoicing context
        // FI 20171102 [XXXXX] Modify
        // PL 20181203 [24362] Add DTDISABLED for new AK_ACTORROLE unique constraint
        // EG [XXXXX][WI437] Nouvelles options de filtrage des données sur les référentiels
        private static void GetSQL(string pSource, SelectedColumnsEnum pSelectedColumns, ReferentialsReferential pReferential, string pSQLWhere,
            bool pIsForExecuteDataAdapter, bool pIsSelectDistinct,
            bool pIsWithColumnAlias, bool pIsWithColumnAliasInOrderExpression,
            string pSQLHints,
            out ArrayList opAlChildSQLselect, out ArrayList opAlChildTableName, out QueryParameters opSQLQuery, out string opSQLWhere, out SQL_Criteria opSQL_Criteria, out string opSQLOrderBy, out bool isQueryWithSubTotal)
        {
            if (pReferential.CmptLevelSpecified && pReferential.CmptLevel == "2.5")
                pSelectedColumns = SelectedColumnsEnum.All;

            #region Initialisation
            #region Variables
            SQLWhere sqlWhere = new SQLWhere();
            string sql_Select = SQLCst.SELECT;
            if (StrFunc.IsFilled(pSQLHints) && (pSQLHints != "~") && DataHelper.IsDbOracle(pSource))
                sql_Select += @"/*+ " + pSQLHints.Trim() + @" */ ";
            if (pIsSelectDistinct)
                sql_Select += SQLCst.DISTINCT.TrimStart(' ');
            string sql_Where = string.Empty;

            string sql_OrderBy = string.Empty;
            string sql_From = string.Empty;
            string sql_Join = string.Empty;
            string sql_Head_Join = string.Empty;
            string sql_Head_WhereJoin = string.Empty;
            string sql_Query = string.Empty;

            // 20090901 RD
            // Le but est de constituer une requête du genre:
            //
            // select sql_select from sql_from
            // union all
            // select sql_SelectGBFirst from (sql_SelectGB1 from sql_from group by sql_GroupBy1)
            // union all
            // select sql_SelectGBFirst from (sql_SelectGB2 from sql_from group by sql_GroupBy2)
            // ...
            // order by sql_orderby

            string sql_SelectGBFirst = sql_Select;
            string sql_SelectGB = sql_Select;
            string sql_GroupBy = string.Empty;
            string sql_SelectGBSort = string.Empty;
            string sql_SelectSort = string.Empty;
            //
            string sql_SelectAdditional = string.Empty;
            string sql_SelectGBAdditional = string.Empty;
            //
            // RD 20091102 / Utilisation de sqlColumn
            SQLReferentialColumn sqlColumn;
            //
            SQL_Criteria sql_Criteria = null;
            Cst.OTCml_TBL tblExtlId = (pReferential.IsDataKeyField_String ? Cst.OTCml_TBL.EXTLIDS : Cst.OTCml_TBL.EXTLID);
            #endregion Variables
            //
            string tableName = pReferential.TableName;
            //CC/PL 20120703 Utilisation de la table à la place de la vue (Vue et ADO.Net incompatible en Oracle®)
            if (pIsForExecuteDataAdapter)
            {
                if (tableName.StartsWith("VW_"))
                    tableName = tableName.Remove(0, 3);
                else if (tableName.StartsWith("EVW_"))
                    tableName = tableName.Remove(0, 4);
            }
            string aliasTableName = SQLCst.TBLMAIN;
            //
            if (pReferential.AliasTableNameSpecified)
                aliasTableName = pReferential.AliasTableName;

            //PL 20110627 Pour la gestion des référentiels basés sur une query dans l'élément SQLSelect (ex. QUOTE_ETD_H_DAILY)
            if ((!pIsForExecuteDataAdapter) && pReferential.SQLSelectSpecified)
                sql_From = SQLCst.X_FROM + "(" + pReferential.SQLSelectCommand + ") " + aliasTableName;
            else
                sql_From = SQLCst.FROM_DBO + tableName + " " + aliasTableName;
            //
            int nbColumn = 0;
            //
            string sql_DefaultOrderBy = string.Empty;
            int nbColumn_DefaultOrderBy = 0;

            //ArrayList listJoinTable = new ArrayList();

            opAlChildTableName = new ArrayList();
            opAlChildSQLselect = new ArrayList();
            //
            bool isWithGroupBy = false;

            // MF 20120430 ruptures with groupingset
            Cst.GroupingSet groupingSet = Cst.GroupingSet.Unknown;
            //
            for (int i = 0; i < pReferential.Column.Length; i++)
            {
                if (pReferential.Column[i].GroupBySpecified)
                {
                    if (pReferential.Column[i].GroupBy.IsGroupBy)
                        isWithGroupBy = true;

                    // MF 20120430 ruptures with groupingset
                    groupingSet = pReferential.Column[i].GroupBy.GroupingSet | groupingSet;

                    //
                    if (isWithGroupBy && Cst.IsWithSubTotal(groupingSet))
                        break;
                }
            }
            //
            if (pReferential.SQLOrderBySpecified && (false == isWithGroupBy || false == Cst.IsWithTotalOrSubTotal(groupingSet)))
            {
                for (int i = 0; i < pReferential.SQLOrderBy.Length; i++)
                {
                    if (pReferential.SQLOrderBy[i].ColumnNotInReferential && pReferential.SQLOrderBy[i].GroupBySpecified)
                    {
                        if (pReferential.SQLOrderBy[i].GroupBy.IsGroupBy)
                            isWithGroupBy = true;
                        //
                        // MF 20120430 ruptures with groupingset
                        groupingSet = pReferential.SQLOrderBy[i].GroupBy.GroupingSet | groupingSet;
                        //
                        if (isWithGroupBy && Cst.IsWithSubTotal(groupingSet))
                            break;
                    }
                }
            }
            #endregion Initialisation

            #region for (int i=0;i<pReferential.Column.Length;i++)
            for (int i = 0; i < pReferential.Column.Length; i++)
            {
                ReferentialsReferentialColumn rrc = pReferential.Column[i];
                // RD 20091102 / Utilisation de sqlColumn
                sqlColumn = new SQLReferentialColumn();
                //
                if (rrc.IsExternal)
                {
                    bool isColumnUsedOnWhere = false;
                    if (pSelectedColumns == SelectedColumnsEnum.None)
                    {
                        isColumnUsedOnWhere = IsColumnUsedOnWhere(pReferential, rrc.AliasTableName);
                    }
                    if ((pSelectedColumns != SelectedColumnsEnum.None) || isColumnUsedOnWhere)//PLTest2012
                    {
                        #region rrc.IsExternal
                        string sqlRestriction = null;
                        string tblAlias = rrc.AliasTableName;//ex.: eaAAA avec e:Externalid, a:ACTOR, AAA:identifiant dans DEFINEEXTLID

                        //Constitution de la query sur la table pour les éventuelles MAJ
                        StrBuilder SQLSelectChild = new StrBuilder(SQLCst.SELECT);
                        SQLSelectChild += tblAlias + ".VALUE,";
                        SQLSelectChild += tblAlias + ".TABLENAME," + tblAlias + ".IDENTIFIER," + tblAlias + ".ID,";
                        SQLSelectChild += tblAlias + ".IDAINS," + tblAlias + ".DTINS,";
                        SQLSelectChild += tblAlias + ".IDAUPD," + tblAlias + ".DTUPD";
                        SQLSelectChild += Cst.CrLf;
                        SQLSelectChild += SQLCst.FROM_DBO + tblExtlId.ToString() + " " + tblAlias + Cst.CrLf;
                        if (!pIsForExecuteDataAdapter)
                        {
                            sqlRestriction = tblAlias + ".TABLENAME=" + DataHelper.SQLString(rrc.ExternalTableName);
                            sqlRestriction += SQLCst.AND;
                            sqlRestriction += tblAlias + ".IDENTIFIER=" + DataHelper.SQLString(rrc.ExternalIdentifier);
                            //
                            SQLSelectChild += SQLCst.WHERE + sqlRestriction;
                        }
                        opAlChildTableName.Add(tblAlias);
                        opAlChildSQLselect.Add(SQLSelectChild.ToString());

                        //Left join pour la query principale
                        if (!pIsForExecuteDataAdapter)
                        {
                            string sql_TmpJoin = SQLCst.LEFTJOIN_DBO + tblExtlId.ToString() + " " + tblAlias;
                            sql_TmpJoin += SQLCst.ON + "(";
                            // RD 20161121 [22619] Use IndexEXTLID
                            //sql_TmpJoin += tblAlias + ".ID=" + SQLCst.TBLMAIN + "." + pReferential.Column[pReferential.IndexDataKeyField].ColumnName;
                            sql_TmpJoin += tblAlias + ".ID=" + SQLCst.TBLMAIN + "." + pReferential.Column[pReferential.IndexEXTLID].ColumnName;
                            sql_TmpJoin += SQLCst.AND + sqlRestriction;
                            sql_TmpJoin += ")";
                            //
                            sql_Join += Cst.CrLf + sql_TmpJoin;
                            //
                            // RD 20091102 / Utilisation de sqlColumn
                            // RD 20091223 / 16802/ LSTCOLUMN.SQLSELECT / Correction
                            sqlColumn.SetSqlColumnInfo(tblAlias + ".VALUE", "Col" + rrc.ExternalIdentifier);
                            //
                            sql_Select += Cst.CrLf + sqlColumn.SQLSelect + ",";
                            if (isWithGroupBy)
                                sqlColumn.GetSqlGroupBy(ref sql_SelectGBFirst, ref sql_SelectGB, ref sql_GroupBy);
                        }
                        #endregion rrc.IsExternal
                    }
                }
                else if (rrc.IsRole)
                {
                    if (pSelectedColumns != SelectedColumnsEnum.None)//PLTest2012
                    {
                        #region rrc.IsRole
                        string sqlRestriction = null;
                        string tblAlias = rrc.AliasTableName;                       //ex.: raSYSADMIN avec r:Role, a:ACTORROLE, SYSADMIN:role SYSADMIN
                        string tbl = pReferential.RoleTableName.Value;              //ex.:  "ACTORROLE"
                        string tblRole = "ROLE" + tbl.Substring(0, tbl.Length - 4); //ex.:  "ROLEACTOR"
                        string idcol = "ID" + tblRole;                              //ex.:  "IDROLEACTOR"
                        string idcolPK = pReferential.Column[pReferential.IndexDataKeyField].ColumnName; //ex.:  "IDA"

                        if (!rrc.DataField.StartsWith("IDA"))//Glop en DUR
                        {
                            //Constitution de la query sur la table pour les éventuelles MAJ
                            string SQLSelectChild = SQLCst.SELECT;
                            SQLSelectChild += "{0}." + idcol + ",{0}." + idcolPK + " as ID,";
                            if (tbl == Cst.OTCml_TBL.ACTORROLE.ToString())
                                SQLSelectChild += "{0}.IDA_ACTOR,";//Car inclu dans la AK
                            else if ((tbl == Cst.OTCml_TBL.GINSTRROLE.ToString()) || (tbl == Cst.OTCml_TBL.GCONTRACTROLE.ToString()))
                                SQLSelectChild += "{0}.IDA,";//Car inclu dans la UK
                            SQLSelectChild += "{0}.DTENABLED,";//Car not null
                            if (tbl == Cst.OTCml_TBL.ACTORROLE.ToString())
                                SQLSelectChild += "{0}.DTDISABLED,";//Car inclu dans la AK depuis "Spheres v8.0"
                            SQLSelectChild += "{0}.IDAINS,{0}.DTINS,{0}.IDAUPD,{0}.DTUPD" + Cst.CrLf;
                            SQLSelectChild += SQLCst.FROM_DBO + tbl + " {0}" + Cst.CrLf;
                            if (!pIsForExecuteDataAdapter)
                            {
                                sqlRestriction = "{0}." + idcol + "=" + DataHelper.SQLString(tblAlias.Substring(2));
                                SQLSelectChild += SQLCst.WHERE + sqlRestriction;
                            }
                            opAlChildTableName.Add(tblAlias);
                            opAlChildSQLselect.Add(String.Format(SQLSelectChild, tblAlias));
                        }
                        //NB: Left join pour la query principale: Aucun car données absentes du Datagrid
                        #endregion rrc.IsRole
                    }
                }
                else if (rrc.IsItem)
                {
                    #region rrc.IsItem
                    //rrc.AliasTableName: Constitué de 3 données (cf. DeserializeXML_ForModeRW())
                    //ex.: ii99 avec "i" en dur pour "Item", premier caractère lower() de la table (ex. "i" pour INSTRUMENT), valeur de la donnée (ex. 99 pour un IDI)
                    string tblAlias = rrc.AliasTableName;
                    string dataValue = rrc.AliasTableName.Substring(2);
                    string idcolPK = pReferential.Column[pReferential.IndexDataKeyField].ColumnName; //ex.:  "IDGINSTR" pour le référentiel "GINSTR"
                    //PL 20170913 [23409] Newness
                    if (pReferential.Items.datacolumnnameSpecified)
                    {
                        idcolPK = pReferential.Items.datacolumnname;    //ex. CONTRACTG --> "IDXC" instead of "IDDC" and "IDCC"
                    }

                    //Constitution de l'ordre SQL SELECT sur la table concernée
                    string SQLSelectChild = SQLCst.SELECT;
                    SQLSelectChild += "{0}." + pReferential.Items.columnname + ",{0}." + idcolPK + " as ID,{0}.IDAINS,{0}.DTINS,{0}.IDAUPD,{0}.DTUPD";
                    if (pReferential.Items.addcolumnsSpecified && !String.IsNullOrEmpty(pReferential.Items.addcolumns))
                    {
                        SQLSelectChild += ",{0}." + pReferential.Items.addcolumns.Replace(",", ",{0}.");
                    }
                    //PL 20170913 [23409] Newness
                    if (pReferential.Items.addcolumnkeySpecified)
                    {
                        SQLSelectChild += ",{0}." + pReferential.Items.addcolumnkey;
                    }

                    SQLSelectChild += Cst.CrLf;
                    SQLSelectChild += SQLCst.FROM_DBO + pReferential.Items.tablename + " {0}" + Cst.CrLf;
                    if (!pIsForExecuteDataAdapter)
                    {
                        SQLSelectChild += SQLCst.WHERE + "{0}." + pReferential.Items.columnname + "=" + DataHelper.SQLString(dataValue);
                    }
                    //PL 20170913 [23409] Newness
                    if (pReferential.Items.addcolumnkeySpecified && pReferential.Items.addcolumnkeyvalueSpecified)
                    {
                        SQLSelectChild += (pIsForExecuteDataAdapter ? SQLCst.WHERE : SQLCst.AND) + "{0}." + pReferential.Items.addcolumnkey + "=" + DataHelper.SQLString(pReferential.Items.addcolumnkeyvalue);
                    }

                    opAlChildTableName.Add(tblAlias);
                    opAlChildSQLselect.Add(String.Format(SQLSelectChild, tblAlias));

                    //NB: Left join pour la query principale: Aucun, car données non présentes dans le Datagrid.
                    #endregion rrc.IsItem
                }
                else
                {
                    #region !rrc.IsAdditionalData
                    #region Compteur de colonnes
                    if (nbColumn % 10 == 0)
                    {
                        sql_Select += Cst.CrLf;
                        //
                        if (isWithGroupBy)
                        {
                            sql_SelectGBFirst += Cst.CrLf;
                            sql_SelectGB += Cst.CrLf;
                        }
                    }
                    //
                    nbColumn++;
                    #endregion
                    //
                    if (TypeData.IsTypeImage(rrc.DataType.value))
                    {
                        #region Cas particulier des colonnes de type Image
                        //20100506 PL Add if (!pIsForExecuteDataAdapter), car le CaseWhenIsNull pose pb en Oracle où ODP.Net génère un parameter mais pas la colonne
                        //            NB: On pourr as epsoser la question d'ailleurs s'il est utile de conserver cette colonne dans le select de load du datagrid ?   
                        if (!pIsForExecuteDataAdapter)
                        {
                            // RD 20091102 / Utilisation de sqlColumn
                            sqlColumn.ConstructSqlColumn(pSource, rrc, false, false, false);
                            //
                            // RD 20091029 - PL 20091029
                            // Pas de chargement des donnée de Type Image pour des raisons de performances, 
                            // en plus une donnée de type Image n'est affichée ni sur le réferential ni sur le formulaire
                            //
                            // RD 20091102 / Le "case when" est demandé par FI, pour une exploitation dans le chargement des Confirm en Zip
                            // RD 20091214 / 16802/ LSTCOLUMN.SQLSELECT
                            string sqlTextColumn = sqlColumn.GetSqlColumnName_CaseWhenIsNull("Binary");
                            //
                            sql_Select += sqlTextColumn + ", ";
                            //
                            if (isWithGroupBy)
                                sqlColumn.GetSqlGroupBy(sqlTextColumn, ref sql_SelectGBFirst, ref sql_SelectGB); // La colonne de type IMAGE n'est pas incluse dans la clause Group by
                        }
                        #endregion
                    }
                    else if (TypeData.IsTypeText(rrc.DataType.value) &&
                            (pIsSelectDistinct ||
                            (pReferential.IsGrid && rrc.IsHideInDataGridSpecified && rrc.IsHideInDataGrid && rrc.LengthInDataGridSpecified && (rrc.LengthInDataGrid == -1))))
                    {
                        #region Cas particulier des colonnes de type Text
                        // RD 20100506 Idem que la correction de Pascal ci-dessus ( problème du CaseWhenIsNull en Oracle/ODP.Net)
                        if (!pIsForExecuteDataAdapter)
                        {
                            // RD 20091102 / Utilisation de sqlColumn
                            sqlColumn.ConstructSqlColumn(pSource, rrc, false, false, false);
                            //
                            //Warning: En mode "select" pas d'affichage des données Text car incompatibilté avec un "select distinct"
                            //
                            // 20091030 RD
                            // Pour des raisons de performances, Pas de chargement des données :
                            // 1- En mode DataGrid ( par contre bien les charger en mode formulaire)
                            // 2- ET De type Text 
                            // 3- ET avec IsHideInDataGrid = True
                            // 4- ET avec LengthInDataGrid = -1
                            //
                            // RD 20091102 / Le "case when" est demandé par FI, pour une exploitation dans le chargement des Confirm en Zip
                            // RD 20091214 / 16802/ LSTCOLUMN.SQLSELECT
                            string sqlTextColumn = sqlColumn.GetSqlColumnName_CaseWhenIsNull("Text");
                            //
                            sql_Select += sqlTextColumn + ", ";
                            //
                            if (isWithGroupBy)
                                sqlColumn.GetSqlGroupBy(sqlTextColumn, ref sql_SelectGBFirst, ref sql_SelectGB); // La colonne de type TEXT n'est pas incluse dans la clause Group by
                        }
                        #endregion
                    }
                    else
                    {
                        #region Select
                        // RD 20091102 / Utilisation de sqlColumn
                        sqlColumn.ConstructSqlColumn(pSource, rrc, true, pIsWithColumnAlias, pIsWithColumnAliasInOrderExpression);

                        bool isColumnToAddInSelect = false;

                        if (pIsForExecuteDataAdapter)
                        {
                            if (rrc.IsAliasEqualToMasterAliasTableName(pReferential.AliasTableName) && rrc.IsNotVirtualColumn)
                                isColumnToAddInSelect = true;
                        }
                        else
                        {
                            isColumnToAddInSelect = true;

                            // RD 20130319 [18508] --------------------------------------------------------------------------------------
                            // Ce problème apparait sur les référentiels avec des colonnes cachées sur le DataGrid (<IsHideInDataGrid>true</IsHideInDataGrid>)
                            // Car ce n'est pas les mêmes requêtes qui sont générées pour:
                            // - le Chargement du DataGrid 
                            // - et pour l'Enregistrement des modifications (ExecuteDataAdapter).
                            //
                            // Correction apportée en v2.x uniquement : Mettre en commentaire le code ci-dessous
                            // -----------------------------------------------------------------------------------------------------------
                            //if (pSelectedColumns != SelectedColumnsEnum.All)//PLTest2012
                            //{
                            //    bool isColumnUsedOnWhere = rrc.ExistsRelation && IsColumnUsedOnWhere(pReferential, rrc.Relation[0].AliasTableName);
                            //    if (!isColumnUsedOnWhere)
                            //    {
                            //        if (pSelectedColumns == SelectedColumnsEnum.None)
                            //            isColumnToAddInSelect = false;
                            //        //PL 20120831 Add test on !rrc.IsHide (si une colonne est IsHideInDataGrid et IsHide, c'est qu'elle est destinée à un usage particulier, docn on la load)
                            //        //else if ((rrc.IsHideInDataGrid) && (!rrc.IsDataKeyField) && (rrc.ColumnName != "ROWATTRIBUT") && (rrc.ColumnName != "ROWVERSION"))
                            //        else if ((rrc.IsHideInDataGrid && !rrc.IsHide) && (!rrc.IsDataKeyField) && (rrc.ColumnName != "ROWATTRIBUT") && (rrc.ColumnName != "ROWVERSION"))
                            //            isColumnToAddInSelect = false;

                            //        if (!isColumnToAddInSelect)
                            //        {
                            //            sql_Select += SQLCst.NULL + SQLCst.AS + sqlColumn.sqlColumnAlias + ",";
                            //            System.Diagnostics.Debug.WriteLine(sqlColumn.sqlColumnAlias);
                            //        }
                            //    }
                            //}
                        }

                        // 20090828 RD / Pour alléger le code
                        if (isColumnToAddInSelect)
                        {
                            // RD 20091102 / Utilisation de sqlColumn
                            sql_Select += sqlColumn.SQLSelect + ",";

                            if (isWithGroupBy)
                                sqlColumn.GetSqlGroupBy(ref sql_SelectGBFirst, ref sql_SelectGB, ref sql_GroupBy);
                        }
                        #endregion Select

                        #region ExistsRelation
                        if (!pIsForExecuteDataAdapter && rrc.ExistsRelation)
                        {
                            // RD 20091102 / Utilisation de sqlColumn
                            string columnNameForRelation = sqlColumn.SqlColumnName;

                            #region Compteur de colonnes
                            if (nbColumn % 10 == 0)
                            {
                                sql_Select += Cst.CrLf;
                                //
                                if (isWithGroupBy)
                                {
                                    sql_SelectGBFirst += Cst.CrLf;
                                    sql_SelectGB += Cst.CrLf;
                                }
                            }
                            //
                            nbColumn++;
                            #endregion


                            string aliasTableJoin = rrc.Relation[0].AliasTableName;
                            // FI 20171025 [23533] devient une property
                            /*
                            rrc.Relation[0].RelationColumnSQLName = string.Empty;

                            // RD 20091223 / 16802/ LSTCOLUMN.SQLSELECT / Correction
                            if (-1 == rrc.Relation[0].ColumnSelect[0].ColumnName.IndexOf("."))
                                rrc.Relation[0].RelationColumnSQLName = aliasTableJoin + "_" + rrc.Relation[0].ColumnSelect[0].ColumnName;
                            else
                                rrc.Relation[0].RelationColumnSQLName = rrc.Relation[0].ColumnSelect[0].ColumnName;
                            */
                            sqlColumn.SetSqlColumnInfo(rrc.Relation[0].ColumnSelect[0].ColumnName, rrc.Relation[0].RelationColumnSQLName.ToUpper(), aliasTableJoin);

                            if ((pSelectedColumns != SelectedColumnsEnum.All) && (!isColumnToAddInSelect)
                                //PL Cas spécifiques... (géré en dur)
                                && (rrc.Relation[0].AliasTableName != "vw_tiu2" /* FEESCHEDULE */)
                                && (rrc.Relation[0].AliasTableName != "vw_ti2"  /* FEESCHEDULE */)
                                && (rrc.Relation[0].AliasTableName != "vw_tc2"  /* FEESCHEDULE */)
                                && (rrc.Relation[0].AliasTableName != "vw_2"    /* ACCKEYVALUE, ... */)
                                && (rrc.Relation[0].AliasTableName != "vw_mc2"  /* CNFMESSAGE, TS_CNFMESSAGE */)
                                && (rrc.Relation[0].AliasTableName != "vw_tm2"  /* INVOICINGRULES */)
                                && (rrc.Relation[0].AliasTableName != "evenum"  /* TAXEVENT */)
                                && (rrc.Relation[0].TableName != "PRODUCT"      /* INSTRUMENT */)
                                )
                            {
                                //PLTest2012
                                sql_Select += SQLCst.NULL + SQLCst.AS + sqlColumn.SqlColumnAlias + ",";

                                System.Diagnostics.Debug.WriteLine("--> " + sqlColumn.SqlColumnAlias);
                            }
                            else
                            {
                                sql_Select += sqlColumn.SQLSelect + ",";

                                if (isWithGroupBy)
                                    sqlColumn.GetSqlGroupBy(ref sql_SelectGBFirst, ref sql_SelectGB, ref sql_GroupBy);

                                // RD 20111230 Pour ne pas remplacer rrc.IsOrderBy.order s'il est spécifié dans le XML
                                if ((rrc.IsOrderBySpecified == false) || (rrc.IsOrderBy.orderSpecified == false))
                                    sqlColumn.SetSqlColumnOrderBy(pIsWithColumnAlias);

                                sql_Join += Cst.CrLf;
                                //PL 20081209 Utilisation systématique de LEFTJOIN_DBO (Utile pour les colonnes IsMandatory parfois Disabled, ex.: GINSTRROLE.IDA)
                                //sql_Join += (rrc.IsMandatory ? SQLCst.INNERJOIN_DBO:SQLCst.LEFTJOIN_DBO) + tableJoin + " " + aliasTableJoin; 
                                //sql_Join += SQLCst.LEFTJOIN_DBO + rrc.Relation[0].TableName + (rrc.Relation[0].TableName == aliasTableJoin ? string.Empty : " " + aliasTableJoin);
                                //PL 20151127 WARNING! Réutilisation de IsMandatory pour définir INNER JOIN ou LEFTJOIN_DBO 
                                //                     L'usage du LEFT modifie dans certains cas le plan d'exécution, dégradant les performances (ex. Consulation des événements depuis un trade - EVENTASSET.xml)
                                //                     Par ailleurs, GINSTRROLE.IDA dispose aujourd'hui de IsMandatory=false
                                //                     A voir à l'usage si certains référentiels posent problème... 
                                // EG 20231114 [WI736] On ne construit pas les jointure d'une relation si ApplyJoin = False
                                if (rrc.IsApplyJoin)
                                {
                                    sql_Join += (rrc.IsMandatory ? SQLCst.INNERJOIN_DBO : SQLCst.LEFTJOIN_DBO)
                                                      + rrc.Relation[0].TableName + (rrc.Relation[0].TableName == aliasTableJoin ? string.Empty : " " + aliasTableJoin);
                                    sql_Join += SQLCst.ON + "(" + aliasTableJoin + "." + rrc.Relation[0].ColumnRelation[0].ColumnName + "=" + columnNameForRelation + ")";
                                    #region Apply SQL Condition on main query
                                    //------------------------------------------------------------
                                    //PL 20100913 Add
                                    //------------------------------------------------------------
                                    if (rrc.Relation[0].Condition != null)
                                    {
                                        for (int nbCondition = 0; nbCondition < rrc.Relation[0].Condition.Length; nbCondition++)
                                        {
                                            //PL 20190513 New feature: apply = "GRID"
                                            if (rrc.Relation[0].Condition[nbCondition].applySpecified
                                                && (rrc.Relation[0].Condition[nbCondition].apply == "ALL" || rrc.Relation[0].Condition[nbCondition].apply == "GRID")
                                                && rrc.Relation[0].Condition[nbCondition].SQLWhereSpecified)
                                            {
                                                sql_Join += SQLCst.AND + "(";
                                                //PL 20110914
                                                if (!rrc.Relation[0].Condition[nbCondition].SQLWhere.StartsWith("("))
                                                    sql_Join += aliasTableJoin + ".";//PL: Bidouille à revoir...
                                                sql_Join += rrc.Relation[0].Condition[nbCondition].SQLWhere + ")";
                                            }
                                        }
                                    }
                                    //------------------------------------------------------------
                                    #endregion Apply SQL Condition on main query
                                }
                            }
                        }
                        #endregion ExistsRelation

                        if (!pIsForExecuteDataAdapter)
                        {
                            #region sql_OrderBy & sql_DefaultOrderBy
                            if (rrc.IsOrderBySpecified
                                &&
                                (
                                (BoolFunc.IsTrue(rrc.IsOrderBy.Value))
                                ||
                                (rrc.IsOrderBy.Value == SQLCst.ASC.Trim())
                                ||
                                (rrc.IsOrderBy.Value == SQLCst.DESC.Trim())
                                )
                               )
                            {
                                if (isWithGroupBy)
                                    sql_OrderBy += GetColumnSortInGroupBy(sqlColumn, ref sql_SelectSort, ref sql_SelectGBSort);

                                sql_OrderBy += sqlColumn.SqlColumnOrderBy;
                                if (rrc.IsOrderBy.Value.ToLower() == SQLCst.DESC.Trim())
                                    sql_OrderBy += SQLCst.DESC;
                                sql_OrderBy += ",";
                            }
                            //20090429 PL Ajout test sur !pIsSelectDistinct pour éviter un bug SQL (Distinct / Order By)
                            //20090812 FI le bug is Les éléments ORDER BY doivent se retrouver dans la liste de sélection si SELECT DISTINCT est spécifié. 
                            else if ((!pIsSelectDistinct)
                                && (nbColumn_DefaultOrderBy < 1)
                                && (rrc.IsHideInDataGridSpecified && !rrc.IsHideInDataGrid))
                            {
                                if (nbColumn_DefaultOrderBy != 0)
                                    sql_DefaultOrderBy += ",";
                                //
                                // RD 20091102 / Utilisation de sqlColumn
                                if (isWithGroupBy)
                                    sql_DefaultOrderBy += GetColumnSortInGroupBy(sqlColumn, ref sql_SelectSort, ref sql_SelectGBSort);
                                //
                                //20101203 FI
                                sql_DefaultOrderBy += sqlColumn.SqlColumnOrderBy + SQLCst.ASC;
                                //
                                nbColumn_DefaultOrderBy++;
                            }
                            #endregion
                        }
                    }
                    #endregion !rrc.IsAdditionalData
                }
            }
            #endregion for (int i=0;i<pReferential.Column.Length;i++)

            if (StrFunc.IsFilled(pSQLWhere))
            {
                //20070529 PL Astuce pour ne pas charger le Dataset en mode "création" ( cf. InitializeReferentialForForm_2() )
                if (pSQLWhere.IndexOf("'NewRecord'") > 0)
                {
                    sqlWhere.Append(pSQLWhere);
                }
                else
                {
                    //20081110 PL Refactoring pour le "."
                    sqlWhere.Append(SetAliasToSQLWhere(pReferential, pSQLWhere));
                }
            }
            
            if (!pIsForExecuteDataAdapter)
            {
                if (SessionTools.IsRequestTrackEnabled && pReferential.RequestTrackSpecified)
                    CheckReferentialRequestTrack(pReferential.RequestTrack);

                #region SQLJoinSpecified
                if (pReferential.SQLJoinSpecified)
                {
                    for (int i = 0; i < pReferential.SQLJoin.Length; i++)
                    {
                        if (pReferential.SQLJoin[i] != null)
                        {
                            if (pSelectedColumns == SelectedColumnsEnum.None) //PLTest2012
                            {
                                if (!pReferential.SQLJoin[i].Trim().StartsWith(SQLCst.X_LEFT.Trim()))
                                    sql_Head_Join += Cst.CrLf + pReferential.SQLJoin[i];
                            }
                            else
                            {
                                sql_Head_Join += Cst.CrLf + pReferential.SQLJoin[i];
                            }
                        }
                    }
                }
                #endregion SQLJoinSpecified
                #region SQLWhereSpecified
                if (pReferential.SQLWhereSpecified)
                {
                    string source = SessionTools.CS;

                    string colDataType = string.Empty;
                    string tmpColumn = string.Empty;
                    string tempColumnNameOrColumnSQLSelect = string.Empty;
                    string tmpOperator = string.Empty;
                    string tmpValue = string.Empty;
                    string tmpColumnSqlWhere = string.Empty;
                                        
                    SQL_ColumnCriteria[] sql_ColumnCriteria = null;
                    if (pReferential.SQLWhere.Length > 0)
                        sql_ColumnCriteria = new SQL_ColumnCriteria[pReferential.SQLWhere.Length];
                    
                    for (int i = 0; i < pReferential.SQLWhere.Length; i++)
                    {
                        ReferentialsReferentialSQLWhere rrw = pReferential.SQLWhere[i];
                        if (null != rrw)
                        {
                            #region SQLJoinSpecified
                            if (rrw.SQLJoinSpecified)
                            {
                                for (int iJoin = 0; iJoin < rrw.SQLJoin.Length; iJoin++)
                                {
                                    if (rrw.SQLJoin[iJoin] != null)
                                        sql_Head_WhereJoin += Cst.CrLf + rrw.SQLJoin[iJoin];
                                }
                            }
                            #endregion SQLJoinSpecified
                            #region ColumnNameSpecified
                            if (rrw.ColumnNameSpecified)
                            {
                                // FI 20201201 [XXXXX] Call GetSQL_ColumnCriteria
                                sql_ColumnCriteria[i] = GetSQL_ColumnCriteria(pSource, rrw, pReferential);
                            }
                            #endregion ColumnNameSpecified
                            #region SQLWhereSpecified
                            if (rrw.SQLWhereSpecified)
                            {
                                // EG 20120503
                                if (rrw.SQLWhere.Contains(Cst.DA_DEFAULT) && pReferential.dynamicArgsSpecified)
                                {
                                    // EG 201306026 Appel à la méthode de Désérialisation d'un StringDynamicData en chaine
                                    // FI 20200205 [XXXXX] Lecture directe du 1er élément du dictionnaire pReferential.dynamicArgs
                                    //EFS_SerializeInfoBase serializerInfo = new EFS_SerializeInfoBase(typeof(StringDynamicData), pReferential.dynamicArgs[currentArg]);
                                    //StringDynamicData sDa = (StringDynamicData)CacheSerializer.Deserialize(serializerInfo);
                                    //StringDynamicData sDa = ReferentialTools.DeserializeDA(pReferential.xmlDynamicArgs[currentArg]);
                                    sqlWhere.Append(rrw.SQLWhere.Replace(Cst.DA_DEFAULT,
                                        pReferential.dynamicArgs.Values.Where(x => x.source.HasFlag(DynamicDataSourceEnum.URL)).First().value));
                                }
                                else
                                {
                                    // FI 20180502 [23926] Appel à ReplaceSQLCriteria
                                    //sqlWhere.Append(rrw.SQLWhere);
                                    string where = pReferential.ReplaceSQLCriteria(pSource, rrw.SQLWhere);

                                    // FI 20201125 [XXXXX] Mise en commentaire et call ReferentialTools.ReplaceDynamicArgsInChooseExpression
                                    // FI 20180502 [23926] l'Expression where peut contenir des choose
                                    //if (pReferential.dynamicArgsSpecified && where.Contains(@"<choose>"))
                                    //{
                                    //    dynamicArgsType = new TypeBuilder(SessionTools.CS, (from item
                                    //                                                        in pReferential.dynamicArgs
                                    //                                                        select item.Value as StringDynamicData).ToList(), "DynamicDataWhere", "ReferentialsReferential");
                                    //    where = StrFuncExtended.ReplaceChooseExpression2(where, dynamicArgsType.GetNewObject(), true);
                                    //}
                                    where = ReferentialTools.ReplaceDynamicArgsInChooseExpression(pReferential, where);

                                    sqlWhere.Append(where);
                                }
                            }
                            #endregion SQLWhereSpecified
                        }
                    }//end for
                    

                    if (ArrFunc.IsFilled(sql_ColumnCriteria))
                    {
                        // MF 20120410 collation strategy using ICultureParameter
                        // ticket 17743 blocked activity to make Oracle case insensitive via globalization parameters
                        //bool applySimpleCollation = DataHelper.MayUseUpper(SessionTools.CS);
                        //FI 20180906 [24159] Appel à la méthode SystemSettings.IsCollationCI()
                        sql_Criteria = new SQL_Criteria(sql_ColumnCriteria, SystemSettings.IsCollationCI());
                        sqlWhere.Append(sql_Criteria.GetSQLWhere(pSource, SQLCst.TBLMAIN));
                    }
                }
                #endregion SQLWhereSpecified
                #region isValidDataOnly & isDailyUpdDataOnly
                // RD 20120131 
                // Afficher uniquement les données valides
                // EG 20120202 utilisation de aliasTableName à la place de tblMain
                if (pReferential.isValidDataOnly)
                {
                    sqlWhere.Append(OTCmlHelper.GetSQLDataDtEnabled(pSource, aliasTableName, true));
                    //PL 20161124 - RATP 4Eyes - MakingChecking
                    if (pReferential.ExistsMakingChecking)
                        sqlWhere.Append(aliasTableName + ".ISCHK=1");
                }
                else if (pReferential.isUnValidDataOnly)
                {
                    sqlWhere.Append(OTCmlHelper.GetSQLDataDtDisabled(pSource, aliasTableName, true));
                }
                // Afficher uniquement les données mises à jour aujourd’hui (créés ou modifiés)
                //if (pReferential.isDailyUpdDataOnly)
                //    sqlWhere.Append(OTCmlHelper.GetSQLDataDtUpd(pSource, aliasTableName));
                if (pReferential.isDailyUpdDataOnly && pReferential.isDailyNewDataOnly)
                    sqlWhere.Append(OTCmlHelper.GetSQLDataDtUpd(pSource, aliasTableName));
                else if (pReferential.isDailyUpdDataOnly)
                    sqlWhere.Append(OTCmlHelper.GetSQLDataDtUpdOnly(pSource, aliasTableName));
                else if (pReferential.isDailyNewDataOnly)
                    sqlWhere.Append(OTCmlHelper.GetSQLDataDtNewOnly(pSource, aliasTableName));
                else if (pReferential.isDailyUserUpdDataOnly && pReferential.isDailyUserNewDataOnly)
                    sqlWhere.Append(OTCmlHelper.GetSQLDataUserDtUpd(pSource, aliasTableName, SessionTools.Collaborator.Ida));
                else if (pReferential.isDailyUserUpdDataOnly)
                    sqlWhere.Append(OTCmlHelper.GetSQLDataUserDtUpdOnly(pSource, aliasTableName, SessionTools.Collaborator.Ida));
                else if (pReferential.isDailyUserNewDataOnly)
                    sqlWhere.Append(OTCmlHelper.GetSQLDataUserDtNewOnly(pSource, aliasTableName, SessionTools.Collaborator.Ida));

                #endregion
                #region SQLOrderBySpecified
                if (pReferential.SQLOrderBySpecified)
                {
                    //Il existe un tri défini par l'utilisateur (Table LSTORDERBY)
                    sql_OrderBy = string.Empty;
                    sql_SelectSort = string.Empty;
                    sql_SelectGBSort = string.Empty;
                    //
                    for (int i = 0; i < pReferential.SQLOrderBy.Length; i++)
                    {
                        if (pReferential.SQLOrderBy[i] != null)
                        {
                            if (i != 0)
                                sql_OrderBy += ",";
                            //
                            if (pIsWithColumnAlias)
                                sql_OrderBy += pReferential.SQLOrderBy[i].ValueWithAlias;
                            else
                                sql_OrderBy += pReferential.SQLOrderBy[i].Value;
                            //
                            if (StrFunc.IsFilled(pReferential.SQLOrderBy[i].GroupBySort))
                            {
                                sql_SelectSort += pReferential.SQLOrderBy[i].GroupBySort;
                                sql_SelectGBSort += pReferential.SQLOrderBy[i].GroupBySortWithAlias;
                            }
                            // 20090928 RD / Add column in select Clause if does not exist
                            if (pReferential.SQLOrderBy[i].ColumnNotInReferential &&
                                false == TypeData.IsTypeImage(pReferential.SQLOrderBy[i].DataType) &&
                                false == TypeData.IsTypeText(pReferential.SQLOrderBy[i].DataType))
                            {
                                // RD 20091102 / Utilisation de sqlColumn
                                sqlColumn = new SQLReferentialColumn();
                                //
                                // RD 20091216 / 16802/ LSTCOLUMN.SQLSELECT
                                sqlColumn.ConstructSqlColumn(pReferential.SQLOrderBy[i]);
                                //  
                                sql_Select += sqlColumn.SQLSelect + ", ";
                                //
                                if (isWithGroupBy)
                                    sqlColumn.GetSqlGroupBy(pReferential.SQLOrderBy[i].GroupBy, ref sql_SelectGBFirst, ref sql_SelectGB, ref sql_GroupBy);
                            }
                        }
                    }
                }
                if ((sql_OrderBy.Length == 0) && (nbColumn_DefaultOrderBy > 0))
                {
                    //None order --> Set default order
                    sql_OrderBy = sql_DefaultOrderBy;
                }
                //
                #endregion SQLOrderBySpecified
                #region UseStatistic
                //region UseStatistic
                if (pReferential.UseStatisticSpecified && pReferential.UseStatistic)
                {
                    //PLTest2012 A étudier concernant les Statistic
                    sql_Join += Cst.CrLf + OTCmlHelper.GetSQLJoin_Statistic(tableName, aliasTableName);

                    if (pIsWithColumnAlias)
                        sql_OrderBy += tableName + "_S_USEFREQUENCY";
                    else
                        sql_OrderBy = OTCmlHelper.GetSQLOrderBy_Statistic(pSource, tableName, sql_OrderBy);
                }
                #endregion UseStatistic
                //
                // 20090805 RD 
                // Ajouter de nouvelles colonnes dynamiques :
                //  - ROWSTYLE   : Le style des lignes 
                //  - ROWSTATE   : Le contenu de la colonne caractérisant la ligne
                //  - ISSELECTED : Pour gérer la sélection des lignes une par une à travers une CheckBox sur chaque ligne
                //
                #region sql_SelectAdditional for SQLRowStyle, SQLRowState and sqlIsSelected



                //
                string sqlRowStyle = string.Empty;
                if (pReferential.SQLRowStyleSpecified && StrFunc.IsFilled(pReferential.SQLRowStyle.Value))
                {
                    //PL 20180619
                    //sqlRowStyle = Cst.CrLf + pReferential.SQLRowStyle.Value + SQLCst.AS + "ROWSTYLE, ";
                    sqlRowStyle = Cst.CrLf + StrFunc.Trim_CrLfTabSpace(pReferential.SQLRowStyle.Value) + SQLCst.AS + "ROWSTYLE,";
                }

                string sqlRowState = string.Empty;
                if (pReferential.SQLRowStateSpecified && StrFunc.IsFilled(pReferential.SQLRowState.Value))
                {
                    //PL 20180619
                    //sqlRowState = pReferential.SQLRowState.Value;
                    //sqlRowState = Cst.CrLf + "(" + sqlRowState + ")" + SQLCst.AS + "ROWSTATE,";
                    sqlRowState = (string.IsNullOrEmpty(sqlRowStyle) ? Cst.CrLf : string.Empty)
                                  + StrFunc.Trim_CrLfTabSpace(pReferential.SQLRowState.Value) + SQLCst.AS + "ROWSTATE,";
                }

                // FI 20140519 [19923]  add colum RequestTrackData
                string sqlRequestTrackData = string.Empty;
                if (SessionTools.IsRequestTrackEnabled && pReferential.RequestTrackSpecified)
                {
                    for (int k = 0; k < ArrFunc.Count(pReferential.RequestTrack.RequestTrackData); k++)
                    {
                        ReferentialsReferentialRequestTrackData rtd = pReferential.RequestTrack.RequestTrackData[k];
                        sqlRequestTrackData += Cst.CrLf + rtd.columnGrp.sqlCol + SQLCst.AS + rtd.columnGrp.alias + ",";
                        if (rtd.columnIdASpecified)
                            sqlRequestTrackData += Cst.CrLf + rtd.columnIdA.sqlCol + SQLCst.AS + rtd.columnIdA.alias + ",";
                        if (rtd.columnIdBSpecified)
                            sqlRequestTrackData += Cst.CrLf + rtd.columnIdB.sqlCol + SQLCst.AS + rtd.columnIdB.alias + ",";
                    }
                }


                string sqlIsSelected = string.Empty;
                if (pReferential.SQLCheckSelectedDefaultValueSpecified)// EG 20110906
                    sqlIsSelected = Cst.CrLf + (BoolFunc.IsFalse(pReferential.SQLCheckSelectedDefaultValue) ? "0" : "1");
                else
                    sqlIsSelected = "1";
                sqlIsSelected += SQLCst.AS + "ISSELECTED,";


                sql_SelectAdditional += sqlIsSelected + sqlRowStyle + sqlRowState + sqlRequestTrackData;
                // 20110912 PM
                // Générer les colonnes ROWSTYLE et ROWSTATE vide pour les requêtes avec les données agrégés (GroupBy)
                if (isWithGroupBy)
                {
                    string sqlRowStyleGB = string.Empty;
                    if (pReferential.SQLRowStyleSpecified && StrFunc.IsFilled(pReferential.SQLRowStyle.Value))
                    {
                        sqlRowStyleGB = Cst.CrLf + "''" + SQLCst.AS + "ROWSTYLE,";
                    }

                    string sqlRowStateGB = string.Empty;
                    if (pReferential.SQLRowStateSpecified && StrFunc.IsFilled(pReferential.SQLRowState.Value))
                    {
                        //sqlRowStateGB = Cst.CrLf + "''" + SQLCst.AS + "ROWSTATE,";
                        sqlRowStateGB = (string.IsNullOrEmpty(sqlRowStyleGB) ? Cst.CrLf : string.Empty)
                                        + "''" + SQLCst.AS + "ROWSTATE,";
                    }

                    // FI 20140519 [19923]  add colum RequestTrackData
                    string sqlRequestTrackDataGB = string.Empty;
                    if (SessionTools.IsRequestTrackEnabled && pReferential.RequestTrackSpecified)
                    {
                        for (int k = 0; k < ArrFunc.Count(pReferential.RequestTrack.RequestTrackData); k++)
                        {
                            ReferentialsReferentialRequestTrackData rtd = pReferential.RequestTrack.RequestTrackData[k];
                            sqlRequestTrackDataGB += Cst.CrLf + "''" + SQLCst.AS + rtd.columnGrp.alias + ",";
                            if (pReferential.RequestTrack.RequestTrackData[k].columnIdASpecified)
                                sqlRequestTrackDataGB += Cst.CrLf + "null" + SQLCst.AS + rtd.columnIdA.alias + ",";
                            if (pReferential.RequestTrack.RequestTrackData[k].columnIdBSpecified)
                                sqlRequestTrackDataGB += Cst.CrLf + "null" + SQLCst.AS + rtd.columnIdB.alias + ",";
                        }
                    }
                    string sqlIsSelectedGB = "1" + SQLCst.AS + "ISSELECTED,";

                    sql_SelectGBAdditional += sqlIsSelectedGB + sqlRowStyleGB + sqlRowStateGB + sqlRequestTrackDataGB;
                }
                #endregion
            }
            //
            if (sql_OrderBy.Length > 0)
            {
                if (!sql_OrderBy.Trim().ToLower().StartsWith(SQLCst.ORDERBY.Trim().ToLower()))
                    sql_OrderBy = SQLCst.ORDERBY + sql_OrderBy;
            }

            #region Final
            if (sql_Select == null)
                sql_Select = "*";
            //
            sql_Select += sql_SelectAdditional;
            //
            if (isWithGroupBy)
            {
                sql_Select += Cst.CrLf + sql_SelectSort;
                sql_Select += "0" + SQLCst.AS + sql_GroupByCount + ",0" + SQLCst.AS + sql_GroupByNumber + ",";
                //
                // 20110912 PM
                // Générer les colonnes ROWSTYLE et ROWSTATE vide pour les requêtes avec les données agrégés (GroupBy)
                sql_SelectGBFirst += sql_SelectGBAdditional + Cst.CrLf;
                //
                sql_SelectGBFirst += sql_SelectGBSort;
                sql_SelectGBFirst += sql_GroupByCount + "," + sql_GroupByNumber + ",";
                //
                sql_SelectGB += Cst.CrLf + SQLCst.COUNT_ALL.Trim() + SQLCst.AS + sql_GroupByCount + ",";
                //
                if (sql_OrderBy.Length > 0)
                    sql_OrderBy += ", " + sql_GroupByNumber;
            }

            char[] cTrim = (Cst.CrLf).ToCharArray();
            sql_Select = sql_Select.Trim().TrimEnd(cTrim);
            if (isWithGroupBy)
            {
                sql_SelectGBFirst = sql_SelectGBFirst.Trim().TrimEnd(cTrim);
                sql_SelectGB = sql_SelectGB.Trim().TrimEnd(cTrim);
                sql_GroupBy = sql_GroupBy.Trim().TrimEnd(cTrim);
            }

            cTrim = (",").ToCharArray();
            sql_Select = sql_Select.Trim().TrimEnd(cTrim);
            if (isWithGroupBy)
            {
                sql_SelectGBFirst = sql_SelectGBFirst.Trim().TrimEnd(cTrim);
                sql_SelectGB = sql_SelectGB.Trim().TrimEnd(cTrim);
                sql_GroupBy = sql_GroupBy.Trim().TrimEnd(cTrim);
            }

            if (pSelectedColumns == SelectedColumnsEnum.None)//PLTest2012
                sql_Select = SQLCst.SELECT + SQLCst.COUNT_1;


            /* FI 20201125 [XXXXX] Mise en commentaire et call ReferentialTools.ReplaceDynamicArgsInChooseExpression
            bool isDynamicArgsType_Loaded = false;
            if (pReferential.dynamicArgsSpecified && sql_Select.Contains(@"<choose>"))
            {
                isDynamicArgsType_Loaded = true;
                dynamicArgsType = new TypeBuilder(SessionTools.CS, (from item
                                                                    in pReferential.dynamicArgs
                                                                    select item.Value as StringDynamicData).ToList(), "DynamicDataWhere", "ReferentialsReferential");
                sql_Select = StrFuncExtended.ReplaceChooseExpression2(sql_Select, dynamicArgsType.GetNewObject(), true);
            }
            if (StrFunc.IsFilled(sql_Head_Join))
            {
                if (pReferential.dynamicArgsSpecified && sql_Head_Join.Contains(@"<choose>"))
                {
                    if (!isDynamicArgsType_Loaded)
                        dynamicArgsType = new TypeBuilder(SessionTools.CS, (from item
                                                                            in pReferential.dynamicArgs
                                                                            select item.Value as StringDynamicData).ToList(), "DynamicDataWhere", "ReferentialsReferential");
                    sql_Head_Join = StrFuncExtended.ReplaceChooseExpression2(sql_Head_Join, dynamicArgsType.GetNewObject(), true);
                }
            }
            */
            sql_Select = ReferentialTools.ReplaceDynamicArgsInChooseExpression(pReferential, sql_Select);
            if (StrFunc.IsFilled(sql_Head_Join))
                sql_Head_Join = ReferentialTools.ReplaceDynamicArgsInChooseExpression(pReferential, sql_Head_Join);
            
            //+*+*+*+*+*+*+*+*+*+*+*+*+*+*+*+*+*+*+*+*+*+*+*+*+*+*+*+*+*+*+*+*+*+*+*+*+*+*+*+*+*+*

            sql_Query = sql_Select + Cst.CrLf + sql_From;

            if (StrFunc.IsFilled(sql_Head_Join))
                sql_Query += sql_Head_Join;
            if (StrFunc.IsFilled(sql_Join))
                sql_Query += sql_Join;
            if (StrFunc.IsFilled(sql_Head_WhereJoin))
                sql_Query += sql_Head_WhereJoin;

            cTrim = (",").ToCharArray();
            sql_Where = sqlWhere.ToString();
            if (sql_Where.Length > 0)
                sql_Query += Cst.CrLf + sql_Where.TrimEnd(cTrim);

            #region GroupBy
            // MF 20120430 ruptures with groupingset
            if (isWithGroupBy && StrFunc.IsFilled(sql_GroupBy) && Cst.IsWithTotalOrSubTotal(groupingSet))
            {
                isQueryWithSubTotal = true;

                // MF 20120430 ruptures with groupingset
                if (Cst.IsWithDetails(groupingSet))
                {
                    sql_Query += Cst.CrLf + SQLCst.UNIONALL + Cst.CrLf;
                }
                else
                {
                    sql_Query = String.Empty;
                }

                sql_Query += sql_SelectGBFirst + Cst.CrLf;
                sql_Query += SQLCst.X_FROM + Cst.CrLf;
                //
                sql_Query += "(" + sql_SelectGB + ",1" + SQLCst.AS + sql_GroupByNumber + Cst.CrLf;
                sql_Query += sql_From;
                //
                if (StrFunc.IsFilled(sql_Head_Join))
                    sql_Query += sql_Head_Join;
                if (StrFunc.IsFilled(sql_Join))
                    sql_Query += sql_Join;
                if (StrFunc.IsFilled(sql_Head_WhereJoin))
                    sql_Query += sql_Head_WhereJoin;
                //
                if (sql_Where.Length > 0)
                    sql_Query += Cst.CrLf + sql_Where.TrimEnd(cTrim);
                //
                //sql_Query += Cst.CrLf + SQLCst.GROUPBY + sql_GroupBy + ") tblGroupBy" + Cst.CrLf;
                sql_Query += Cst.CrLf + SQLCst.GROUPBY + sql_GroupBy.GetNormalizedGroupByString() + ") tblGroupBy" + Cst.CrLf;
                //
                // MF 20120430 ruptures with groupingset
                if (Cst.IsWithSubTotal(groupingSet))
                {
                    //string[] allGroupByColumn = sql_GroupBy.Split(",".ToCharArray());
                    string[] allGroupByColumn = sql_GroupBy.GetGroupByColumn();
                    //
                    if (allGroupByColumn.Length > 1)
                    {
                        string newSql_GroupBy = sql_GroupBy;
                        string newSql_SelectGB = sql_SelectGB;
                        //
                        for (int i = allGroupByColumn.Length; i > 1; i--)
                        {
                            int groupByNumber = allGroupByColumn.Length - i + 2;
                            string groupByColumn = allGroupByColumn[i - 1];
                            //
                            newSql_SelectGB = newSql_SelectGB.Replace(groupByColumn.Trim(), SQLCst.NULL);
                            //
                            newSql_GroupBy = newSql_GroupBy.Replace(groupByColumn.Trim(), string.Empty);
                            newSql_GroupBy = newSql_GroupBy.Trim().TrimEnd(cTrim);
                            //
                            sql_Query += Cst.CrLf + SQLCst.UNIONALL + Cst.CrLf;
                            sql_Query += sql_SelectGBFirst + Cst.CrLf;
                            sql_Query += SQLCst.X_FROM + Cst.CrLf;
                            //
                            sql_Query += "(" + newSql_SelectGB + "," + groupByNumber.ToString() + SQLCst.AS + sql_GroupByNumber + Cst.CrLf;
                            sql_Query += sql_From;
                            //
                            if (StrFunc.IsFilled(sql_Head_Join))
                                sql_Query += sql_Head_Join;
                            if (StrFunc.IsFilled(sql_Join))
                                sql_Query += sql_Join;
                            if (StrFunc.IsFilled(sql_Head_WhereJoin))
                                sql_Query += sql_Head_WhereJoin;
                            //
                            if (sql_Where.Length > 0)
                                sql_Query += Cst.CrLf + sql_Where.TrimEnd(cTrim);
                            //
                            //sql_Query += Cst.CrLf + SQLCst.GROUPBY + newSql_GroupBy + ") tblGroupBy_" + groupByNumber.ToString() + Cst.CrLf;
                            sql_Query += Cst.CrLf + SQLCst.GROUPBY + newSql_GroupBy.GetNormalizedGroupByString() + ") tblGroupBy_" + groupByNumber.ToString() + Cst.CrLf;
                        }
                    }
                }
            }
            else
                isQueryWithSubTotal = false;
            #endregion


            //Tip: Si pSQLHints == "~" il s'agit d'un appel depuis GetSQLOrderBy(). 
            //     Pour optimiser, on n'opère pas de "ReplaceChoose" car seul OrderBy est exploité.
            if (string.IsNullOrEmpty(pSQLHints) || (pSQLHints != "~"))
            {
               // FI 20201125 [XXXXX] Mise en commentaire et call ReferentialTools.ReplaceDynamicArgsInChooseExpression
               //if (pReferential.dynamicArgsSpecified && sql_Query.Contains(@"<choose>"))
               //{
               //    dynamicArgsType = new TypeBuilder(SessionTools.CS, (from item
               //                                                        in pReferential.dynamicArgs
               //                                                        select item.Value as StringDynamicData).ToList(), "DynamicDataWhere", "ReferentialsReferential");
               //    sql_Query = StrFuncExtended.ReplaceChooseExpression2(sql_Query, dynamicArgsType.GetNewObject(), true);
               //}

               sql_Query = ReferentialTools.ReplaceDynamicArgsInChooseExpression(pReferential, sql_Query);
            }
            //+*+*+*+*+*+*+*+*+*+*+*+*+*+*+*+*+*+*+*+*+*+*+*+*+*+*+*+*+*+*+*+*+*+*+*+*+*+*+*+*+*+*

            //RD 20120524 - Ne pas mettre la clause OrderBy directement dans la query 
            //La requête suivante ne passe pas: 
            //      select col1 as value1 from tbl1
            //      union all  
            //      select col1 as value1 from tbl2
            //      order by case when value1 = 'xxx' then 'xxxX' else value1 end
            //    
            //Enrichir la requête plus tard pour utiliser la syntaxe ISO: ROW_NUMBER() OVER
            //if (sql_OrderBy.Length > 0)
            //    sql_Query = DataHelper.TransformQueryForOrderBy(sql_Query, sql_OrderBy.TrimEnd(cTrim), true, isQueryWithSubTotal);

            sql_Query = pReferential.ReplaceDynamicArgument2(pSource, sql_Query);
            // FI 20201125 [XXXXX] call consultationCriteria.ReplaceConsultCriteriaKeyword
            sql_Query = ConsultationCriteria.ReplaceConsultCriteriaKeyword(pReferential, sql_Query);
            opSQLQuery = pReferential.ConvertQueryToQueryParameters(pSource, sql_Query);

            opSQLWhere = sql_Where.TrimEnd(cTrim);
            opSQL_Criteria = sql_Criteria;
            opSQLOrderBy = sql_OrderBy.TrimEnd(cTrim);
            #endregion Final
            // 20190503 [XXXXX] 
            //Debug.WriteLine(sql_Query);
        }
        /// <summary>
        /// Retourne TRUE si la colonne est utilisée au sein d'un critère (Where), sur la base de la pérsence de son alias de table. Sinon retorune FALSE.
        /// </summary>
        /// <param name="pReferential"></param>
        /// <param name="pColumn_AliasTableName"></param>
        /// <returns></returns>
        private static bool IsColumnUsedOnWhere(ReferentialsReferential pReferential, string pColumn_AliasTableName)
        {
            bool ret = false;
            if (pReferential.SQLWhereSpecified)
            {
                for (int ii = 0; ii < pReferential.SQLWhere.Length; ii++)
                {
                    ReferentialsReferentialSQLWhere rrw = pReferential.SQLWhere[ii];
                    if (null != rrw)
                    {
                        if (rrw.ColumnNameSpecified && rrw.AliasTableName == pColumn_AliasTableName)
                        {
                            //L'alias de la table de la colonne correspond à l'alias de table du critère courant.
                            ret = true;
                            break;
                        }
                    }
                }
            }
            return ret;
        }
        #endregion

        #region private BuildValueFormated
        /// <summary>
        ///  Formatage SQL de la donnée FK ou PK
        /// </summary>
        /// <param name="pReferential">Class Referential</param>
        /// <param name="pValue">Valeur de la donnée</param>
        /// <param name="pIsDataKeyField">Indicateur de donnée PK (True) ou FK (False)</param>
        /// <returns></returns>
        // FI 20160916 [22471] Modify
        // RD 20161129 [22599] Modify
        public static string BuildValueFormated(ReferentialsReferential pReferential, string pColumnValue, bool pIsDataKeyField)
        {
            string ret = string.Empty;
            if (StrFunc.IsFilled(pColumnValue))
            {
                int index;
                if (pIsDataKeyField)
                    index = pReferential.IndexDataKeyField;
                else
                    index = pReferential.IndexForeignKeyField;

                // FI 20160916 [22471] sécurité/vulnérabilité => Spheres® parser la donnée en entrée afin d'en vérifier son contenu
                // Corrige la faille suivante où le detete était exécuté
                // http://localhost/SpheresWebSite_v5.1/Referential.aspx?T=Referential&O=FEE&M=0&N=0&F=divDtg&PK=IDFEE&PKV=1 delete * from trade&IDMenu=OTC_REF_CHARGE_FEES_FEE
                // Cette même URL génère désormais un bug
                if (index > -1)
                {
                    TypeData.TypeDataEnum typeData = TypeData.GetTypeDataEnum(pReferential.Column[index].DataType.value, false);
                    switch (typeData)
                    {
                        case TypeData.TypeDataEnum.integer:
                        case TypeData.TypeDataEnum.@int:
                            // RD 20161129 [22599] Int (int32) to Long (Int64) 
                            //int intValue = IntFunc.IntValue(pColumnValue);
                            long intValue = IntFunc.IntValue64(pColumnValue);
                            ret = intValue.ToString();
                            break;
                        case TypeData.TypeDataEnum.@string:
                            ret = DataHelper.SQLString(pColumnValue);
                            break;
                        default: // FI 20160916 [22471] Mise en place d'une exception  
                            throw new NotImplementedException(StrFunc.AppendFormat("DataType not implemented (Type:{0})", typeData.ToString()));
                    }
                }
                else // FI 20160916 [22471] Mise en place d'une exception  
                {
                    throw new NotSupportedException((pIsDataKeyField) ? "Column DataKeyField not found (index ==-1)" : "Column ForeignKeyField not found (index ==-1)");
                }
            }
            return ret;
        }
        #endregion
        //
        #region private BuildSqlWhere
        /// <summary>
        /// Genere le SQLWhere à partir de la FK ou de la PK
        /// </summary>
        /// <param name="pReferential"></param>
        /// <param name="pColumn"></param>
        /// <param name="pColumnValue"></param>
        /// <param name="pIsColumnDataKeyField"></param>
        /// <returns></returns>
        /// FI 20160916 [22471] Modify
        /// EG 20221010 [XXXXX][WI452] Déplacer la pavé WITH du select DSP/EDSP en tête de query (usage du SQL: WITHTOMOVE/SQL: ENDWITHTOMOVE)
        private static string BuildSqlWhere(ReferentialsReferential pReferential, string pColumn, string pColumnValue, bool pIsColumnDataKeyField)
        {
            // FI 20160916 [22471] Mise en commentaire 
            //string valueFormated = BuildValueFormated(pReferential, pColumnValue, pIsColumnDataKeyField);
            string ret = string.Empty;
            if (StrFunc.IsFilled(pColumn) && StrFunc.IsFilled(pColumnValue))
            {
                // FI 20160916 [22471] Appel à BuildValueFormated ici + cas particulier en création
                if (pColumn == "'TIP'" && pColumnValue == "'NewRecord'")
                {
                    ret = StrFunc.AppendFormat("{0}={1}", pColumn, pColumnValue);
                }
                else
                {
                    string valueFormated = BuildValueFormated(pReferential, pColumnValue, pIsColumnDataKeyField);
                    ret = pColumn + "=" + valueFormated;
                }
            }
            return ret;
        }
        #endregion
        //
        #region TransformRecursiveQuery
        //PL 20110303 TEST for SQLServer WITH (TBD)
        //EG 20111222 Test SQLServer/Oracle
        ///<summary>
        /// Traitement d'une requête possédant des instructions de récursivité
        /// <para>────────────────────────────────────────────────────────────────────────────</para>
        /// <para>► SQLServer</para> 
        /// <para>────────────────────────────────────────────────────────────────────────────</para>
        /// <para>► PREAMBULE : La sous-requête de récursivité doit être encadrée des tags /* SQL: WITH */ et /* SQL: ENDWITH */</para>
        /// <para>1. La fonction déplace cette sous-requête en tête de requête principale</para>
        /// <para>2. Si présence des tags /* SQL: WITHWHERE */ et /* SQL: ENDWITHWHERE */</para> 
        /// <para>   alors la clause WHERE de la requête principale (FK/PK) de type (column = value) est:</para>
        /// <para>   ● déplacée dans celle de récursivité</para>
        /// <para>   ● remplacée par (column = column)</para>
        /// <para>────────────────────────────────────────────────────────────────────────────</para>
        /// <para>► Oracle</para> 
        /// <para>────────────────────────────────────────────────────────────────────────────</para>
        /// <para>1. Si présence des tags /* SQL: WITHWHERE */ et /* SQL: ENDWITHWHERE */</para> 
        /// <para>   alors la clause WHERE de la requête principale (FK/PK) de type (column = value) est:</para>
        /// <para>   ● déplacée dans celle de récursivité</para>
        /// <para>   ● remplacée par (column = column)</para>
        ///</summary>
        // EG 20230102 [XXXXX][WI452|WI500] Déplacer la pavé WITH du select DSP/EDSP en tête de query (usage du SQL: WITHTOMOVE/SQL: ENDWITHTOMOVE)
        public static string TransformRecursiveQuery(string pCS, ReferentialsReferential pReferential, string pColumn, string pColumnValue,
            bool pIsColumnDataKeyField, string pQuery)
        {

            string query = pQuery;
            DbSvrType serverType = DataHelper.GetDbSvrType(pCS);
            if (DbSvrType.dbSQL == serverType)
            {
                int posWith = query.IndexOf(@"/* SQL: WITH */");
                if (posWith > 0)
                {
                    int posEndWith = query.IndexOf(@"/* SQL: ENDWITH */");
                    string querySource = query;
                    string queryWidth = querySource.Substring(posWith, posEndWith + @"/* SQL: ENDWITH */".Length - posWith + 1);

                    string sqlWhere = BuildSqlWhere(pReferential, pColumn, pColumnValue, pIsColumnDataKeyField);
                    if (StrFunc.IsFilled(sqlWhere))
                    {
                        // EG 20130729 Test IsGrid en commentaire
                        //if (pReferential.IsGrid)
                        //{
                        int posWithWhere = queryWidth.IndexOf(@"/* SQL: WITHWHERE */");
                        if (posWithWhere > 0)
                        {
                            // on inhile la partie du where de la query principale (FK/PK): (column = value) devient (column = column)
                            string sqlWhereSubstitute = sqlWhere.Replace(pColumnValue, SetAliasToSQLWhere(pReferential, pColumn));
                            sqlWhereSubstitute = sqlWhereSubstitute.Replace("'", string.Empty);
                            querySource = querySource.Replace(sqlWhere, sqlWhereSubstitute);
                            // pour ne la conserver que dans le CTE
                            int posEndWithWhere = queryWidth.IndexOf(@"/* SQL: ENDWITHWHERE */");
                            queryWidth = queryWidth.Substring(0, posWithWhere) + sqlWhere + queryWidth.Substring(posEndWithWhere + @"/* SQL: ENDWITHWHERE */".Length);
                            posEndWith = querySource.IndexOf(@"/* SQL: ENDWITH */");
                        }
                        //}
                    }
                    query = queryWidth + querySource.Substring(0, posWith)
                            + querySource.Substring(posEndWith + @"/* SQL: ENDWITH */".Length);

                    // EG 20210813 [XXXXX] [XXXXX] Consultations : Application incorrecte du tri, bug dans la query en cas de présence de plusieurs instructions with.
                    // EG 20210813 on retire la clause WITH et la remplace par une VIRGULE car nous avons ici déjà un WITH dans la query.
                    query = query.Replace("with CTE_ROW_NUM", ", CTE_ROW_NUM");
                }
                // EG 20230102 [XXXXX][WI452|WI500] Déplacer la pavé WITH du select DSP/EDSP en tête de query (usage du SQL: WITHTOMOVE/SQL: ENDWITHTOMOVE)
                posWith = query.IndexOf(@"/* SQL: WITHTOMOVE */");
                if (posWith > 0)
                {
                    int posEndWith = query.IndexOf(@"/* SQL: ENDWITHTOMOVE */");
                    string querySource = query;
                    string queryWidth = querySource.Substring(posWith, posEndWith + @"/* SQL: ENDWITHTOMOVE */".Length - posWith + 1);
                    query = query.Remove(posWith, posEndWith + @"/* SQL: ENDWITHTOMOVE */".Length - posWith + 1);
                    //[26241][WI452] Absense de "with CTE_ROW_NUM" sur le Select d'ouverture du formulaire, à la différence du Select de load du DataGrid
                    //query = query.Replace("with CTE_ROW_NUM", queryWidth + ", CTE_ROW_NUM");
                    query = queryWidth + query.Replace("with CTE_ROW_NUM", ", CTE_ROW_NUM");
                }
            }
            else if (DbSvrType.dbORA == serverType)
            {
                string sqlWhere = BuildSqlWhere(pReferential, pColumn, pColumnValue, pIsColumnDataKeyField);
                if (StrFunc.IsFilled(sqlWhere))
                {
                    if (pReferential.IsGrid)
                    {
                        int posWithWhere = query.IndexOf(@"/* SQL: WITHWHERE */");
                        if (posWithWhere > 0)
                        {
                            // on inhile la partie du where de la query principale (FK/PK): (column = value) devient (column = column)
                            string sqlWhereSubstitute = sqlWhere.Replace(pColumnValue, SetAliasToSQLWhere(pReferential, pColumn));
                            sqlWhereSubstitute = sqlWhereSubstitute.Replace("'", string.Empty);
                            query = query.Replace(sqlWhere, sqlWhereSubstitute);
                            // pour ne la conserver que dans le CTE
                            int posEndWithWhere = query.IndexOf(@"/* SQL: ENDWITHWHERE */");
                            query = query.Substring(0, posWithWhere) + sqlWhere + query.Substring(posEndWithWhere + @"/* SQL: ENDWITHWHERE */".Length);
                        }
                    }
                }
                // EG 20221010 [XXXXX][WI452] Déplacer la pavé WITH du select DSP/EDSP en tête de query (usage du SQL: WITHTOMOVE/SQL: ENDWITHTOMOVE)
                int posWith = query.IndexOf(@"/* SQL: WITHTOMOVE */");
                if (posWith > 0)
                {
                    int posEndWith = query.IndexOf(@"/* SQL: ENDWITHTOMOVE */");
                    string querySource = query;
                    string queryWidth = querySource.Substring(posWith, posEndWith + @"/* SQL: ENDWITHTOMOVE */".Length - posWith + 1);
                    query = query.Remove(posWith, posEndWith + @"/* SQL: ENDWITHTOMOVE */".Length - posWith + 1);
                    //[26241][WI452] Absense de "with CTE_ROW_NUM" sur le Select d'ouverture du formulaire, à la différence du Select de load du DataGrid
                    //query = query.Replace("with CTE_ROW_NUM", queryWidth + ", CTE_ROW_NUM");
                    query = queryWidth + query.Replace("with CTE_ROW_NUM", ", CTE_ROW_NUM");
                }
            }
            // EG 20180528 Grosse merde pour la gestion affichage POSREQUEST via Tracker
            if (pIsColumnDataKeyField)
            {
                int posWithToDelete = query.IndexOf(@"/* SQL: WITHTODELETE */");
                if (posWithToDelete > 0)
                {
                    int posEndWithToDelete = query.IndexOf(@"/* SQL: ENDWITHTODELETE */");
                    query = query.Remove(posWithToDelete, posEndWithToDelete + @"/* SQL: ENDWITHTODELETE */".Length - posWithToDelete + 1);
                }
            }
            return query;
        }
        #endregion TransformRecursiveQuery

        #region private SetAliasToSQLWhere
        private static string SetAliasToSQLWhere(string pAliasTableName, string pSQLWhere)
        {
            string sqlWhere = pSQLWhere;
            bool isexistAliasTable = false;
            int posDot = sqlWhere.IndexOf(".");
            if (posDot >= 0)
            {
                //Verrue temporaire, pour géréer un cas tel que : COLUMN= 'DATA1.0'
                int posQuote = sqlWhere.IndexOf("'");
                if (posQuote >= 0)
                    isexistAliasTable = (posDot < posQuote);
                else
                    isexistAliasTable = true;
            }
            if (false == isexistAliasTable)
                sqlWhere = pAliasTableName + "." + sqlWhere;
            return sqlWhere;
        }
        private static string SetAliasToSQLWhere(ReferentialsReferential pReferential, string pSQLWhere)
        {
            string aliasTableName = SQLCst.TBLMAIN;
            if (pReferential.AliasTableNameSpecified)
                aliasTableName = pReferential.AliasTableName;
            return SetAliasToSQLWhere(aliasTableName, pSQLWhere);
        }
        #endregion private SetAliasToSQLWhere

        #region class SQLSelectParameters
        public class SQLSelectParameters
        {
            #region Constructor(s)
            public SQLSelectParameters(string pSource, ReferentialsReferential pReferential) :
                this(pSource, SelectedColumnsEnum.All, pReferential) { }
            public SQLSelectParameters(string pSource, SelectedColumnsEnum pSelectedColumns, ReferentialsReferential pReferential)
            {
                source = pSource;
                selectedColumns = pSelectedColumns;
                referential = pReferential;

                isForExecuteDataAdapter = false;
                isSelectDistinct = false;
            }
            public SQLSelectParameters(string pSource, ReferentialsReferential pReferential, string pSQLWhere) :
                this(pSource, SelectedColumnsEnum.All, pReferential)
            {
                sqlWhere = pSQLWhere;
            }
            #endregion

            #region Member(s)
            public string source;
            public ReferentialsReferential referential;

            public string sqlWhere;
            public string sqlHints;
            public bool isForExecuteDataAdapter;
            public bool isSelectDistinct;
            public SQLReferentialData.SelectedColumnsEnum selectedColumns;
            #endregion
        }
        #endregion


        /// <summary>
        /// 
        /// </summary>
        /// <param name="pRequestTrack"></param>
        private static void CheckReferentialRequestTrack(ReferentialsReferentialRequestTrack pRequestTrack)
        {

            for (int k = 0; k < ArrFunc.Count(pRequestTrack.RequestTrackData); k++)
            {
                ReferentialsReferentialRequestTrackData rtd = pRequestTrack.RequestTrackData[k];
                if (null == rtd.columnGrp)
                    throw new Exception("RequestTrackData: ColumnGrp doesn't exist");

                if (rtd.columnIdASpecified)
                {
                    if (StrFunc.IsEmpty(rtd.columnIdA.alias) || StrFunc.IsEmpty(rtd.columnIdA.sqlCol))
                    {
                        throw new Exception("RequestTrackData: columnIdA doesn't contains alias or sqlExpresion");
                    }
                }
                if (rtd.columnIdBSpecified)
                {
                    if (StrFunc.IsEmpty(rtd.columnIdB.alias) || StrFunc.IsEmpty(rtd.columnIdB.sqlCol))
                    {
                        throw new Exception("RequestTrackData: columnIdA doesn't contains alias or sqlExpresion");
                    }
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="pCS"></param>
        /// <param name="rrw"></param>
        /// <param name="pReferential"></param>
        /// FI 20140616 [XXXX] add Method
        public static SQL_ColumnCriteria GetSQL_ColumnCriteria(string pCS, ReferentialsReferentialSQLWhere rrw, ReferentialsReferential pReferential)
        {
            if (false == rrw.ColumnNameSpecified)
                throw new Exception("ColumnName is not Specified for rrw (ReferentialsReferentialSQLWhere)");

            SQL_ColumnCriteria sql_ColumnCriteria = null;

            string tmpValue;
            string[] tmpValues;

            string tmpColumn = rrw.ColumnName;
            string tmpColumnSqlWhere = string.Empty;

            ReferentialsReferentialColumn rrc = pReferential[rrw.ColumnName, rrw.AliasTableName];
            if (null != rrc)
                tmpColumnSqlWhere = (rrc.ColumnSqlWhereSpecified ? rrc.ColumnSqlWhere : (rrw.ColumnSQLWhereSpecified ? rrw.ColumnSQLWhere : string.Empty));
            else
                tmpColumnSqlWhere = (rrw.ColumnSQLWhereSpecified ? rrw.ColumnSQLWhere : string.Empty);


            if (rrw.AliasTableNameSpecified && rrw.AliasTableName.Length > 0)
            {
                tmpColumn = rrw.AliasTableName + "." + tmpColumn;
                tmpColumnSqlWhere = tmpColumnSqlWhere.Replace(Cst.DYNAMIC_ALIASTABLE, rrw.AliasTableName);
            }

            string tempColumnNameOrColumnSQLSelect = SQLReferentialData.GetColumnNameOrColumnSelect(pReferential, rrw);


            if (StrFunc.IsFilled(rrw.LstValue))
            {
                // FI 20201201 [XXXXX] Gestion de DA_DEFAUL
                if ((rrw.LstValue == Cst.DA_DEFAULT) && pReferential.dynamicArgsSpecified)
                {

                    // FI 201003 [pReferential.dynamicArgs n'est constitué que de donnée au format XML]
                    // EG 20130626 Appel à la méthode de Désérialisation d'un StringDynamicData en chaine
                    // FI 20200205 [XXXXX] Lecture directe du 1er élément du dictionnaire pReferential.dynamicArgs
                    //EFS_SerializeInfoBase serializerInfo = new EFS_SerializeInfoBase(typeof(StringDynamicData), pReferential.dynamicArgs[currentArg]);
                    //StringDynamicData sDa = (StringDynamicData)CacheSerializer.Deserialize(serializerInfo);
                    //StringDynamicData sDa = ReferentialTools.DeserializeDA(pReferential.xmlDynamicArgs[currentArg]);
                    tmpValue = pReferential.dynamicArgs.Values.Where(x => x.source.HasFlag(DynamicDataSourceEnum.URL)).First().value;
                }
                else
                {
                    tmpValue = rrw.LstValue;
                }

                tmpValues = StrFunc.StringArrayList.StringListToStringArray(tmpValue);
                for (int j = 0; j < tmpValues.Length; j++)
                {
                    if (tmpValues[j].StartsWith(Cst.DA_START))
                        tmpValues[j] = pReferential.ReplaceDynamicArgument2(pCS, tmpValues[j]);
                }
                tmpValue = ArrFunc.GetStringList(tmpValues, ";");
            }
            else
            {
                tmpValue = string.Empty;
            }
            
            // FI 20190327 [24603] Alimentation de sqlDatatype et sqlDataInput
            SQL_ColumnCriteriaDataType sqlDatatype = new SQL_ColumnCriteriaDataType();
            SQL_ColumnCriteriaInput sqlDataInput = new SQL_ColumnCriteriaInput();
            TypeData.TypeDataEnum datatype = TypeData.GetTypeDataEnum(rrw.DataType.value);
            if (datatype == TypeData.TypeDataEnum.datetime && (rrw.DataType.datakindSpecified && rrw.DataType.datakind == Cst.DataKind.Timestamp)
                && rrw.DataType.tzdbidSpecified)
            {
                sqlDatatype = new SQL_ColumnCriteriaDataType(datatype, rrw.DataType.tzdbid);
                sqlDataInput = new SQL_ColumnCriteriaInput(tmpValue, SessionTools.GetCriteriaTimeZone());
            }
            else
            {
                sqlDatatype = new SQL_ColumnCriteriaDataType(datatype);
                sqlDataInput = new SQL_ColumnCriteriaInput(tmpValue);
            }

            sql_ColumnCriteria = new SQL_ColumnCriteria(sqlDatatype, tempColumnNameOrColumnSQLSelect, tmpColumnSqlWhere, rrw.Operator, sqlDataInput);


            return sql_ColumnCriteria;
        }
    }
    #endregion class SQLReferentialData

    #region public class EFSSyndicationFeed
    /// <summary>
    /// Classe d'utilisation des SyndicationFeed (RSS, Atom)
    /// </summary>
    public sealed class EFSSyndicationFeed
    {
        #region public enum SyndicationFeedFormatEnum
        public enum SyndicationFeedFormatEnum
        {
            ALL, RSS20, Atom10
        }
        #endregion
        #region public enum SyndicationFeedTypeEnum
        public enum SyndicationFeedTypeEnum
        {
            SALESNEWS,      //SALES, FpML/FIXML version, ...
            SOFTWARENEWS,   //Release, Patch, Bug...
            BUSINESSNEWS,   //Corporate actions, ...
        }
        #endregion

        #region public SyndicationFeed
        /// <summary>
        /// Get a SyndicationFeed from SYNDICATIONFEED/SYNDICATIONITEM tables.
        /// </summary>
        /// <param name="pCs"></param>
        /// <param name="pSyndicationFeedType"></param>
        /// <returns></returns>
        // EG [25500] Customer Portal / EFS WebSite : Refactoring de la gestion de la mise à jour des flux RSS (Syndication)
        public static SyndicationFeed GetSyndicationFeed(string pCs, SyndicationFeedTypeEnum pSyndicationFeedType, string pCulture)
        {
            SyndicationFeed feed = null;
            List<SyndicationItem> items = new List<SyndicationItem>();
            int idSyndicationFeed = 0;
            string syndicationFeedLinks = string.Empty;

            string sql = SQLCst.SELECT + "IDSYNDICATIONFEED,LINKS,COPYRIGHT,AUTHORS,GENERATOR,IMAGEURL";
            sql += "," + DataHelper.SQLIsNull(pCs, "TITLE_" + pCulture, "TITLE_EN", "TITLE");
            sql += "," + DataHelper.SQLIsNull(pCs, "DESCRIPTION_" + pCulture, "DESCRIPTION_EN", "DESCRIPTION");
            sql += SQLCst.FROM_DBO + "SYNDICATIONFEED" + Cst.CrLf;
            sql += SQLCst.WHERE + "FEEDTYPE=" + DataHelper.SQLString(pSyndicationFeedType.ToString());
            using (IDataReader dr = DataHelper.ExecuteReader(pCs, CommandType.Text, sql))
            {
                if (dr.Read())
                {
                    idSyndicationFeed = Convert.ToInt32(dr["IDSYNDICATIONFEED"]);
                    //TBD Multi-Links (separator ";")
                    syndicationFeedLinks = Convert.ToString(dr["LINKS"]);

                    feed = SyndicationTools.AddFeed(Convert.ToString(dr["TITLE"]), Convert.ToString(dr["DESCRIPTION"]), Convert.ToString(dr["LINKS"]),
                        Convert.ToString(dr["AUTHORS"]), pCulture, Convert.ToString(dr["COPYRIGHT"]), Convert.ToString(dr["GENERATOR"]), Convert.ToString(dr["IMAGEURL"]));
                        
                }
            }

            if (0 < idSyndicationFeed)
            {
                sql = SQLCst.SELECT + "IDSYNDICATIONITEM,LINKS,PUBLISHDATE,CATEGORIES" + Cst.CrLf;
                sql += "," + DataHelper.SQLIsNull(pCs, "TITLE_" + pCulture, "TITLE_EN", "TITLE");
                sql += "," + DataHelper.SQLIsNull(pCs, "SUMMARY_" + pCulture, "SUMMARY_EN", "SUMMARY");
                sql += ", ENCLOSURE_URI, ENCLOSURE_LENGTH, ENCLOSURE_MEDIATYPE, COMMENTS";
                sql += SQLCst.FROM_DBO + "SYNDICATIONITEM" + Cst.CrLf;
                sql += SQLCst.WHERE + "IDSYNDICATIONFEED=" + idSyndicationFeed.ToString();
                sql += SQLCst.AND + "ISENABLED=1" + Cst.CrLf;
                sql += SQLCst.ORDERBY + "PUBLISHDATE" + SQLCst.DESC;

                using (IDataReader dr = DataHelper.ExecuteReader(pCs, CommandType.Text, sql))
                {
                    while (dr.Read())
                    {
                        SyndicationItem item = SyndicationTools.AddItem(dr["IDSYNDICATIONITEM"], dr["TITLE"], dr["SUMMARY"],
                StrFunc.IsFilled(Convert.ToString(dr["LINKS"])) ? Convert.ToString(dr["LINKS"]) : syndicationFeedLinks, dr["PUBLISHDATE"], dr["CATEGORIES"]);

                        SyndicationTools.SetEnclosure(item, dr["ENCLOSURE_URI"], dr["ENCLOSURE_MEDIATYPE"], dr["ENCLOSURE_LENGTH"]);

                        SyndicationTools.SetComment(item, dr["COMMENTS"]);

                        items.Add(item);
                    }
                    feed.Items = items;
                }
            }
            return feed;
        }
        #endregion
    }
    #endregion

    /// <summary>
    /// helper class containing all the extensions targeting the group by sql command, for the referentiel namespace.
    /// These extentions use a group by string formatted with special characters. Any group by field must be encapsulated with {}. 
    /// Ex: group by {testfield}, {testfield2}
    /// </summary>
    public static class GroupByExtensions
    {
        static readonly Regex parseSpecialCharacters = new Regex(@"\s*{([\s\w\d:_\.,""'-@/\(\)\[\]]+)}\s*", RegexOptions.IgnoreCase);

        // EG 20160404 Migration vs2013
        //static string replace = "$1";

        public static string GetNormalizedGroupByString(this string pSQLGroupBy)
        {
            string[] fields = GetGroupByColumn(pSQLGroupBy);

            return fields.Aggregate((curr, next) => next != null ? String.Concat(curr, ",", next) : curr);
        }

        public static string[] GetGroupByColumn(this string pSQLGroupBy)
        {
            MatchCollection matches = parseSpecialCharacters.Matches(pSQLGroupBy);

            string[] fieldsgroupby = new string[matches.Count];

            for (int idx = 0; idx < matches.Count; idx++)
            {
                fieldsgroupby[idx] = matches[idx].Groups[1].Value;
            }

            return fieldsgroupby;
        }

    } 
}
