<?xml version="1.0" encoding="utf-16"?>
<!-- FI 20131128 [19255] OfficialSettlement or OfficialClose management -->
<!-- FI 20200901 [25468] use of GetUTCDateTimeSys -->
<xsl:stylesheet xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
                xmlns:msxsl="urn:schemas-microsoft-com:xslt"
                version="1.0">

  <xsl:output method="xml" omit-xml-declaration="no" encoding="UTF-8" indent="no" media-type="text/xml; charset=ISO-8859-1"/>

  <!-- Includes-->
  <!-- 20111021 MF RiskData -->
  <xsl:include href="ClosingPriceImport-EUREX-RiskDataTableTemplates.xsl"/>

  <!-- 20111110 PM Parameter PRICEONLY -->
  <!-- the parameter PRICEONLY enables/disables the risk datas import  -->
  <xsl:variable name ="vIsRiskDataImport" >
    <xsl:choose>
      <xsl:when test="/iotask/parameters/parameter[contains(@id, 'PRICEONLY')] and /iotask/parameters/parameter[@id='PRICEONLY']='false'">true</xsl:when>
      <!-- TODO - à revoir par Fab  -->
      <xsl:otherwise>true</xsl:otherwise>
    </xsl:choose>
  </xsl:variable>

  <xsl:decimal-format name="decimalFormat" decimal-separator="." />

  <xsl:template match="/iotask">
    <iotask>
      <xsl:attribute name="id">
        <xsl:value-of select="@id"/>
      </xsl:attribute>
      <xsl:attribute name="name">
        <xsl:value-of select="@name"/>
      </xsl:attribute>
      <xsl:attribute name="displayname">
        <xsl:value-of select="@displayname"/>
      </xsl:attribute>
      <xsl:attribute name="loglevel">
        <xsl:value-of select="@loglevel"/>
      </xsl:attribute>
      <xsl:attribute name="commitmode">
        <xsl:value-of select="@commitmode"/>
      </xsl:attribute>
      <xsl:apply-templates select="parameters[1]"/>
      <xsl:apply-templates select="iotaskdet[1]"/>
    </iotask>
  </xsl:template>
  <xsl:template match="parameters">
    <parameters>
      <xsl:for-each select="parameter" >
        <parameter>
          <xsl:attribute name="id">
            <xsl:value-of select="@id"/>
          </xsl:attribute>
          <xsl:attribute name="name">
            <xsl:value-of select="@name"/>
          </xsl:attribute>
          <xsl:attribute name="displayname">
            <xsl:value-of select="@displayname"/>
          </xsl:attribute>
          <xsl:attribute name="direction">
            <xsl:value-of select="@direction"/>
          </xsl:attribute>
          <xsl:attribute name="datatype">
            <xsl:value-of select="@datatype"/>
          </xsl:attribute>
          <xsl:value-of select="."/>
        </parameter>
      </xsl:for-each>
    </parameters>
  </xsl:template>
  <xsl:template match="iotaskdet">
    <iotaskdet>
      <xsl:attribute name="id">
        <xsl:value-of select="@id"/>
      </xsl:attribute>
      <xsl:attribute name="loglevel">
        <xsl:value-of select="@loglevel"/>
      </xsl:attribute>
      <xsl:attribute name="commitmode">
        <xsl:value-of select="@commitmode"/>
      </xsl:attribute>
      <xsl:apply-templates select="ioinput[1]"/>
    </iotaskdet>
  </xsl:template>
  <xsl:template match="ioinput">
    <ioinput>
      <xsl:attribute name="id">
        <xsl:value-of select="@id"/>
      </xsl:attribute>
      <xsl:attribute name="name">
        <xsl:value-of select="@name"/>
      </xsl:attribute>
      <xsl:attribute name="displayname">
        <xsl:value-of select="@displayname"/>
      </xsl:attribute>
      <xsl:attribute name="loglevel">
        <xsl:value-of select="@loglevel"/>
      </xsl:attribute>
      <xsl:attribute name="commitmode">
        <xsl:value-of select="@commitmode"/>
      </xsl:attribute>
      <xsl:apply-templates select="file[1]"/>
    </ioinput>
  </xsl:template>
  <!--//-->
  <!-- ================================================== -->
  <!--        Main Template                               -->
  <!-- ================================================== -->
  <xsl:template match="file">
    <file>

      <xsl:attribute name="name">
        <xsl:value-of select="@name"/>
      </xsl:attribute>
      <xsl:attribute name="folder">
        <xsl:value-of select="@folder"/>
      </xsl:attribute>
      <xsl:attribute name="date">
        <xsl:value-of select="@date"/>
      </xsl:attribute>
      <xsl:attribute name="size">
        <xsl:value-of select="@size"/>
      </xsl:attribute>

      <!-- Market - Par Defaut EUREX (Correspond a EXCHANGEACRONYM de l'EUREX) -->
      <xsl:variable name ="vMarket" >EUR</xsl:variable>

      <!-- Recuperation des donnees ne figurant qu'en fin de fichier ( RecordId( Rtc = '*EOF*)  ) -->
      <!-- Business Date sur Row avec level = 0-->
      <xsl:variable name ="vBusinessDate" >
        <xsl:value-of select="r[@lv=0 and d[@n='RTC' and @v='*EOF*']]/d[@n='BusDt']/@v"/>
      </xsl:variable>

      <!-- Margin Class Records : Tous les Row de type 'M', avec level = 1-->
      <xsl:for-each select="r[d[@n='RTC' and @v='M']]">

        <!-- Product Records : Tous les Row de type 'P', avec level = 2-->
        <xsl:for-each select="r[d[@n='RTC' and @v='P']]">

          <!-- PROD-ID-COD : Product ID -->
          <xsl:variable name ="vContractSymbol" >
            <xsl:value-of select="d[@n='PrdCd']/@v"/>
          </xsl:variable>

          <!-- EXER-PRC-DECIMALS : Number of decimals in the exercise price -->
          <xsl:variable name="vStrikeDivisor">
            <xsl:call-template name="StrikePriceDiv">
              <!-- StrkNbDec -->
              <xsl:with-param name="pHowManyZero" select="d[@n='StrkNbDec']/@v"/>
            </xsl:call-template>
          </xsl:variable>

          <!-- PROD-TIC-SIZE : Tick size for the product -->
          <xsl:variable name="vTickSize">
            <xsl:value-of select="d[@n='PrdTk']/@v"/>
          </xsl:variable>

          <!-- PROD-TIC-VAL : Tick value for the product -->
          <xsl:variable name="vTickValue">
            <xsl:value-of select="d[@n='PrdTkV']/@v"/>
          </xsl:variable>

          <!-- Mise à jour de la table DERIVATIVECONTRACT (et de la table PARAMSEUREX_CONTRACTS lorsque le paramètre CLEARINGORGANIZATION existe) -->
          <xsl:call-template name="Update_DC">
            <xsl:with-param name="pMarket" select="$vMarket"/>
            <xsl:with-param name="pContractSymbol" select="$vContractSymbol"/>
            <xsl:with-param name="pTickSize" select="$vTickSize"/>
            <xsl:with-param name="pTickValue" select="$vTickValue"/>
            <xsl:with-param name="pBusinessDate" select="$vBusinessDate"/>
          </xsl:call-template>

          <!-- Expiry Records : Tous les Row de type 'E', avec level = 3-->
          <xsl:for-each select="r[d[@n='RTC' and @v='E']]">
            <!-- SERI-EXP-DAT : Expiration Year+Month of the options series or futures contract YYMM -->
            <!-- Maturity Month  
                 Remarque: SerExp est une maturité au format 'AAMM' on rajoute le siecle avant qui est '20'
                           car dans spheres le format d'une maturité est 'CCMMAA' -->
            <xsl:variable name ="vMaturityMonth" >
              <xsl:value-of select="concat('20',d[@n='SerExp']/@v)"/>
            </xsl:variable>

            <!-- SERI-CLAS-COD : Class code for option series or a blank for futures contracts. 
            Field values include:“C“ – Call,“P“ – Put“,“ - Future -->
            <xsl:variable name ="vPutCall" >
              <xsl:choose>
                <xsl:when test="d[@n='PrdTyp']/@v= 'P'">0</xsl:when>
                <xsl:when test="d[@n='PrdTyp']/@v= 'C'">1</xsl:when>
                <xsl:otherwise></xsl:otherwise>
              </xsl:choose>
            </xsl:variable>

            <!-- Category : F for Future , O for Option  -->
            <xsl:variable name ="vCategory" >
              <xsl:choose>
                <xsl:when test="string-length($vPutCall) > 0">O</xsl:when>
                <xsl:when test="string-length($vPutCall) = 0">F</xsl:when>
                <xsl:otherwise></xsl:otherwise>
              </xsl:choose>
            </xsl:variable>

            <!-- Traitement de chaque row ayant pour RecordId(RTC) : RTC => S (Series Record)
                 Remarque le record de type S(Series Record) qui déclanchera l'ecriture dans QUOTE_ETD_H c'est seulement lui qui est pris en
                 compte dans la boucle -->
            <!-- Series Records : Tous les Row de type 'S', avec level = 4-->
            <xsl:for-each select="r[d[@n='RTC' and @v='S']]">
              <r uc="true" id="{@id}" src="{@src}">

                <!--EXER-PRC : Price at which an option contract may be exercised-->
                <xsl:variable name ="vStrike" >
                  <xsl:value-of select="format-number(number(d[@n='Strk']/@v) div $vStrikeDivisor, '0.00#######', 'decimalFormat')" />
                </xsl:variable>

                <!--SERI-VERS-NO : Version number assigned to the series -->
                <xsl:variable name="vVersion">
                  <xsl:value-of select="d[@n='Ver']/@v"/>
                </xsl:variable>

                <!--SERI-REF-PRC : Serie reference price-->
                <xsl:variable name="vClosingPrice">
                  <xsl:value-of select="d[@n='Price']/@v"/>
                </xsl:variable>

                <!--20111214 FL  debut -->
                <!-- Id Asset : Identifiant de l'Asset -->
                <xsl:variable name="vIdAsset">
                  <xsl:value-of select="d[@n='IdAs']/@v"/>
                </xsl:variable>
                <!-- Id Und Asset : Identifiant de l'Asset du sous jacent -->
                <xsl:variable name="vIdUndAsset">
                  <xsl:value-of select="d[@n='IdAsUnd']/@v"/>
                </xsl:variable>
                <!-- Table Und Quote : Nom de la table de cotation du sous jacent-->
                <xsl:variable name="vTableUndQuote">
                  <xsl:value-of select="d[@n='TbUQt']/@v"/>
                </xsl:variable>
                <!--20111214 FL  fin -->

                <!-- Optimisation de la mise à jours de la table de cotation QUOTE_ETD_H si l'asset n'existe pas on sort le plus tot possible du process de mise 
                     à jours de manière a limiter de select .... car il sont inutile caron aura pas d'insertion -->
                <pms>
                  <!--Tous les Assets pour lesquels,:
                      •	il existe au moins un trade négocié sur cet Asset
                      •	Ou bien, il existe au moins un trade négocié sur une option sur Future, dont le SS-J est cet Asset. 
                      -->

                  <pm n="IDASSET_Trade_SsjOptOnFutTrade" v="{$vIdAsset}"/>

                  <xsl:if test="number($vVersion)>0">
                    <!--Tous les Assets existenats.-->
                    <pm n="IDASSET_All">
                      <xsl:call-template name="SQLQueryAsset_ETD">
                        <xsl:with-param name="pResultColumn" select="'IDASSET'"/>
                        <xsl:with-param name="pMarket" select="$vMarket"/>
                        <xsl:with-param name="pContractSymbol" select="$vContractSymbol"/>
                        <xsl:with-param name="pCategory" select="$vCategory"/>
                        <xsl:with-param name="pVersion" select="$vVersion"/>
                        <xsl:with-param name="pMaturityMonth" select="$vMaturityMonth"/>
                        <xsl:with-param name="pPutCall" select="$vPutCall"/>
                        <xsl:with-param name="pStrike" select="$vStrike"/>
                      </xsl:call-template>
                    </pm>
                    <pm n="IDDC{$vCategory}">
                      <xsl:call-template name="SQLQueryDerivativeContract">
                        <xsl:with-param name="pResultColumn" select="'IDDC'"/>
                        <xsl:with-param name="pMarket" select="$vMarket"/>
                        <xsl:with-param name="pContractSymbol" select="$vContractSymbol"/>
                        <xsl:with-param name="pCategory" select="$vCategory"/>
                        <xsl:with-param name="pVersion" select="$vVersion"/>
                      </xsl:call-template>
                    </pm>
                  </xsl:if>

                  <pm n="DatetimeRDBMS">
                    <sl fn="GetUTCDateTimeSys()" />
                  </pm>
                  <pm n="UserId">
                    <sl fn="GetUserId()" />
                  </pm>
                </pms>

                <!-- Mise à jours de la table de cotation DERIVATIVECONTRACT et ASSET_ETD -->
                <xsl:if test="number($vVersion)>0">

                  <!-- Mise à jour de la table DERIVATIVECONTRACT, la colonne MINPRICEINCR -->
                  <xsl:call-template name="SQLTableDC_Update">
                    <xsl:with-param name="pMarket" select="$vMarket"/>
                    <xsl:with-param name="pContractSymbol" select="$vContractSymbol"/>
                    <xsl:with-param name="pTickSize" select="$vTickSize"/>
                    <xsl:with-param name="pTickValue" select="$vTickValue"/>
                    <xsl:with-param name="pCategory" select="$vCategory"/>
                    <xsl:with-param name="pVersion" select="$vVersion"/>
                    <xsl:with-param name="pRows_S" select="."/>
                    <xsl:with-param name="pSeqNum" select="1"/>
                  </xsl:call-template>

                  <!-- SECU-TRD-UNT-NO : The quantity of the underlying instrument traded per contract. -->
                  <xsl:variable name="vTradingUnit">
                    <xsl:value-of select="d[@n='TrdUnt']/@v"/>
                  </xsl:variable>

                  <!-- Calculate the multiplier : Tic Value/Tic Size * Trading Unit-->
                  <xsl:variable name="vMultiplier">
                    <xsl:call-template name="MultiplierProcess">
                      <xsl:with-param name="pTickSize" select="$vTickSize"/>
                      <xsl:with-param name="pTickValue" select="$vTickValue"/>
                      <xsl:with-param name="pTradingUnit" select="$vTradingUnit"/>
                    </xsl:call-template>
                  </xsl:variable>

                  <!-- Mise à jour de la table ASSET_ETD, la colonne CONTRACTMULTIPLIER -->
                  <tbl n="ASSET_ETD" a="U" sn="2">
                    <c n="IDASSET" dk="true" t="i" v="parameters.IDASSET_All">
                      <ctls>
                        <ctl a="RejectRow" rt="true" lt="None">
                          <sl fn="ISNULL()"/>
                        </ctl>
                        <ctl a="RejectColumn" rt="true" lt="None" v="true"/>
                      </ctls>
                    </c>
                    <!-- RD 20130405 [18561] -->
                    <!-- désormais, la mise à jour de la colonne CONTRACTMULTIPLIER est conditionnée par:
                            DERIVATIVECONTRACT.ISAUTOSSETING (Autoriser la mise à jour automatique du contrat)
                         et non par: 
                            DERIVATIVECONTRACT.ISAUTOSSETINGASSET (Autoriser la création et la mise à jour automatique des actifs)-->
                    <c n="ISAUTOSETTING" t="b">
                      <xsl:call-template name="SQLQueryDerivativeContract">
                        <xsl:with-param name="pResultColumn" select="'ISAUTOSETTING'"/>
                        <xsl:with-param name="pMarket" select="$vMarket"/>
                        <xsl:with-param name="pContractSymbol" select="$vContractSymbol"/>
                        <xsl:with-param name="pCategory" select="$vCategory"/>
                        <xsl:with-param name="pVersion" select="$vVersion"/>
                      </xsl:call-template>
                      <ctls>
                        <ctl a="RejectRow" rt="true" lt="None">
                          <sl fn="ISNULL()"/>
                        </ctl>
                        <ctl a="RejectColumn" rt="true" lt="None" v="true"/>
                      </ctls>
                    </c>

                    <!-- Mise à jours de la colonne CONTRACTMULTIPLIER:
                    - pour tous les ASSET_ETD existants
                    - avec version du DC (DERIVATIVECONTRACT.CONTRACTATTRIBUTE) > 0 -->
                    <c n="CONTRACTMULTIPLIER" dku="true" t="dc" v="{$vMultiplier}"/>

                    <c n="DTUPD" t="dt" v="parameters.DatetimeRDBMS"/>
                    <c n="IDAUPD" t="i" v="parameters.UserId"/>
                  </tbl>
                </xsl:if>

                <!-- Mise à jours de la table de cotation QUOTE_ETD_H -->
                <tbl n="QUOTE_ETD_H" a="IU">
                  <xsl:attribute name="sn">
                    <xsl:choose>
                      <xsl:when test="number($vVersion)>0">3</xsl:when>
                      <xsl:otherwise>1</xsl:otherwise>
                    </xsl:choose>
                  </xsl:attribute>

                  <!-- mq n="QuotationHandlingMQueue" a="IU">
                    <xsl:if test="number($vVersion)>0">
                      <p n="IsCashFlowsVal" t="b" v="true"/>
                    </xsl:if>
                  </mq -->
                  <c n="ISEXISTINTRADEINSTRUMENT" t="b" v="parameters.IDASSET_Trade_SsjOptOnFutTrade">
                    <ctls>
                      <ctl a="RejectRow" rt="true" lt="None">
                        <sl fn="ISNULL()"/>
                      </ctl>
                      <ctl a="RejectColumn" rt="true" lt="None" v="true">
                        <!--<logInfo status="INFO" isexception="false">
                        <message>
                          &lt;b&gt;Asset found.&lt;/b&gt;
                          Market:&lt;b&gt;<xsl:value-of select="$Market"/>&lt;/b&gt;
                          Contract:&lt;b&gt;<xsl:value-of select="$ContractSymbol"/>&lt;/b&gt;
                          Version:&lt;b&gt;<xsl:value-of select="$Version"/>&lt;/b&gt;
                          Maturity:&lt;b&gt;<xsl:value-of select="$MaturityMonth"/>&lt;/b&gt;<xsl:if test="string-length($PutCall) > 0">
                            PutCall:&lt;b&gt;<xsl:value-of select="$PutCall"/>&lt;/b&gt;
                            Strike:&lt;b&gt;<xsl:value-of select="$Strike"/>&lt;/b&gt;
                          </xsl:if>
                        </message>
                      </logInfo>-->
                      </ctl>
                    </ctls>
                  </c>

                  <c n="IDMARKETENV" dk="true">
                    <sql cd="select" rt="IDMARKETENV">
                      select IDMARKETENV
                      from MARKETENV
                      where ISDEFAULT = @ISDEFAULT
                      <p n="ISDEFAULT" t="b" v="'1'"/>
                    </sql>
                  </c>
                  <c n="IDVALSCENARIO" dk="true">
                    <sql cd="select" rt="IDVALSCENARIO">
                      select v.IDVALSCENARIO, 1 as colorder
                      from VALSCENARIO v
                      inner join MARKETENV m on (m.IDMARKETENV = v.IDMARKETENV and m.ISDEFAULT  = @ISDEFAULT)
                      where v.ISDEFAULT  = @ISDEFAULT
                      union
                      select v.IDVALSCENARIO, 2 as colorder
                      from VALSCENARIO v
                      where v.ISDEFAULT = @ISDEFAULT and v.IDMARKETENV is null
                      order by colorder asc
                      <p n="ISDEFAULT" t="b" v="'1'"/>
                    </sql>
                  </c>
                  <c n="IDASSET" dk="true" t="i" v="parameters.IDASSET_Trade_SsjOptOnFutTrade"/>

                  <c n="IDC" dku="true" v="null"/>
                  <c n="IDM" dku="true" t="i" >
                    <xsl:call-template name="SQLQueryDerivativeContract">
                      <xsl:with-param name="pResultColumn" select="'IDM'"/>
                      <xsl:with-param name="pMarket" select="$vMarket"/>
                      <xsl:with-param name="pContractSymbol" select="$vContractSymbol"/>
                      <xsl:with-param name="pCategory" select="$vCategory"/>
                      <xsl:with-param name="pVersion" select="$vVersion"/>
                    </xsl:call-template>
                  </c>
                  <c n="TIME" dk="true" t="d" f="yyyy-MM-dd" v="{$vBusinessDate}">
                    <ctls>
                      <ctl a="RejectRow" rt="false" >
                        <sl fn="IsDate()" />
                        <li st="err" ex="true" msg="Invalid Type"/>
                      </ctl>
                    </ctls>
                  </c>
                  <c n="VALUE" dku="true" t="dc" v="{$vClosingPrice}"/>
                  <c n="SPREADVALUE" dku="true" t="dc" v="0"/>
                  <c n="QUOTEUNIT" dku="true" v="Price"/>
                  <!-- FI 20110627 [17490] OfficialClose price -->
                  <c n="QUOTESIDE" dk="true" dku="true" v="OfficialClose" />
                  <c n="QUOTETIMING" dku="true" v="Close"/>
                  <c n="ASSETMEASURE" v="MarketQuote"/>
                  <c n="CASHFLOWTYPE" dk="true" v="null"/>
                  <c n="ISENABLED" dku="true" t="b" v="1"/>
                  <!-- FI 20110627 [17490] EuroFinanceSystems provider -->
                  <!-- FI 20110627 [17490] ClearingOrganization provider -->
                  <c n="SOURCE" v="ClearingOrganization" />

                  <c n="DTUPD" t="dt" v="parameters.DatetimeRDBMS">
                    <ctls>
                      <ctl a="RejectColumn" rt="true" >
                        <sl fn="IsInsert()" />
                      </ctl>
                    </ctls>
                  </c>
                  <c n="IDAUPD" t="i" v="parameters.UserId">
                    <ctls>
                      <ctl a="RejectColumn" rt="true" >
                        <sl fn="IsInsert()" />
                      </ctl>
                    </ctls>
                  </c>
                  <c n="DTINS" t="dt" v="parameters.DatetimeRDBMS">
                    <ctls>
                      <ctl a="RejectColumn" rt="true" >
                        <sl fn="IsUpdate()" />
                      </ctl>
                    </ctls>
                  </c>
                  <c n="IDAINS" t="i" v="parameters.UserId">
                    <ctls>
                      <ctl a="RejectColumn" rt="true" >
                        <sl fn="IsUpdate()" />
                      </ctl>
                    </ctls>
                  </c>
                  <c n="EXTLLINK" v="null"/>
                </tbl>

                <!-- 20111021 START MF RiskData -->
                <xsl:if test="$vIsRiskDataImport = 'true'">

                  <xsl:variable name="vUndPrice" select="d[@n='UndPrice']/@v"/>

                  <!-- RD 20130827 [18834] Gestion de la checkbox "Importation systématique des cours" -->
                  <!-- FI 20131128 [19255] 
                       Le prix est nécessairement de type OfficialSettlement si option sur indice
                       Dans la réduction des fichiers:
                          -  à l'échéance, Spheres® remplace le prix présent par le prix EDSP (prix calculé)
                          -  sinon Spheres® remplace le prix présent par zéro, le prix est alors ignoré (le prix sera récupérer sur les assets futures)
                       
                       Le prix est de type OfficialClose dans les autres cas
                  -->
                  <xsl:variable name="vQuoteSide">
                    <xsl:choose>
                      <xsl:when test="($vCategory='O') and ($vTableUndQuote='QUOTE_INDEX_H')">
                        <!-- Pour une ligne Option sur indice, le prix du sous jacent est de type EDSP
                             La prix sera ignoré s'il vaut zéro -->
                        <xsl:value-of select="'OfficialSettlement'"/>
                      </xsl:when>
                      <xsl:otherwise>
                        <!-- Pour une ligne Future, le prix du sous jacent est de type DSP-->
                        <xsl:value-of select="'OfficialClose'"/>
                      </xsl:otherwise>
                    </xsl:choose>
                  </xsl:variable>

                  <xsl:call-template name="InsertUnderlyingQuotes">
                    <xsl:with-param name="pSequenceNumber" select="'4'"/>
                    <xsl:with-param name="pIdUndAsset" select="$vIdUndAsset"/>
                    <xsl:with-param name="pTableUndQuote" select="$vTableUndQuote"/>
                    <xsl:with-param name="pBusinessDate" select="$vBusinessDate"/>
                    <xsl:with-param name="pCurrency" select="$gNull"/>
                    <xsl:with-param name="pValue" select="$vUndPrice"/>
                    <xsl:with-param name="pQuoteSide" select="$vQuoteSide"/>
                    <xsl:with-param name="pDatetimeRDBMS" select="'parameters.DatetimeRDBMS'"/>
                    <xsl:with-param name="pUserId" select="'parameters.UserId'"/>
                  </xsl:call-template>
                </xsl:if>
                <!-- 20111021 END MF RiskData -->
              </r>
            </xsl:for-each>
          </xsl:for-each>
        </xsl:for-each>
      </xsl:for-each>
    </file>
  </xsl:template>

  <!--//-->
  <!-- ================================================== -->
  <!--        Tools Templates                             -->
  <!-- ================================================== -->
  <!-- Mise à jour de la table DERIVATIVECONTRACT pour les contrats avec:
  1- CONTRACTSYMBOL = pContractSymbol
  2- CONTRACTATTRIBUTE = 0
  3- Qu'ils soient Future ou Option-->
  <xsl:template name="Update_DC">
    <xsl:param name="pMarket"/>
    <xsl:param name="pContractSymbol"/>
    <xsl:param name="pTickSize"/>
    <xsl:param name="pTickValue"/>
    <xsl:param name="pBusinessDate"/>

    <!-- Option en Version=0-->
    <xsl:variable name="vFirst_Rows_S_Version0_Option">
      <xsl:copy-of select="r[d[@n='RTC' and @v='E'] and d[@n='PrdTyp' and (@v='P' or @v='C')]][1]/r[d[@n='RTC' and @v='S'] and number(d[@n='Ver']/@v)=0][1]"/>
    </xsl:variable>
    <!-- Future en Version=0-->
    <xsl:variable name="vFirst_Rows_S_Version0_Future">
      <xsl:copy-of select="r[d[@n='RTC' and @v='E'] and d[@n='PrdTyp' and not(@v='P' or @v='C')]][1]/r[d[@n='RTC' and @v='S'] and number(d[@n='Ver']/@v)=0][1]"/>
    </xsl:variable>
    <!--//-->
    <xsl:if test="(count(msxsl:node-set($vFirst_Rows_S_Version0_Option)/r) > 0) or (count(msxsl:node-set($vFirst_Rows_S_Version0_Future)/r) > 0)">
      <r uc="true" id="{@id}" src="{@src}">
        <pms>
          <xsl:if test="count(msxsl:node-set($vFirst_Rows_S_Version0_Option)/r) > 0">
            <pm n="IDDCO">
              <xsl:call-template name="SQLQueryDerivativeContract">
                <xsl:with-param name="pResultColumn" select="'IDDC'"/>
                <xsl:with-param name="pMarket" select="$pMarket"/>
                <xsl:with-param name="pContractSymbol" select="$pContractSymbol"/>
                <xsl:with-param name="pCategory" select="'O'"/>
                <xsl:with-param name="pVersion" select="'0'"/>
              </xsl:call-template>
            </pm>
          </xsl:if>
          <xsl:if test="count(msxsl:node-set($vFirst_Rows_S_Version0_Future)/r) > 0">
            <pm n="IDDCF">
              <xsl:call-template name="SQLQueryDerivativeContract">
                <xsl:with-param name="pResultColumn" select="'IDDC'"/>
                <xsl:with-param name="pMarket" select="$pMarket"/>
                <xsl:with-param name="pContractSymbol" select="$pContractSymbol"/>
                <xsl:with-param name="pCategory" select="'F'"/>
                <xsl:with-param name="pVersion" select="'0'"/>
              </xsl:call-template>
            </pm>
          </xsl:if>
          <pm n="DatetimeRDBMS">
            <sl fn="GetUTCDateTimeSys()" />
          </pm>
          <pm n="UserId">
            <sl fn="GetUserId()" />
          </pm>
        </pms>

        <xsl:if test="count(msxsl:node-set($vFirst_Rows_S_Version0_Option)/r) > 0">
          <xsl:call-template name="SQLTableDC_Update">
            <xsl:with-param name="pMarket" select="$pMarket"/>
            <xsl:with-param name="pContractSymbol" select="$pContractSymbol"/>
            <xsl:with-param name="pTickSize" select="$pTickSize"/>
            <xsl:with-param name="pTickValue" select="$pTickValue"/>
            <xsl:with-param name="pCategory" select="'O'"/>
            <xsl:with-param name="pVersion" select="'0'"/>
            <xsl:with-param name="pDatetimeRDBMS" select="parameters.DatetimeRDBMS"/>
            <xsl:with-param name="pUserId" select="parameters.UserId"/>
            <xsl:with-param name="pBusinessDate" select="$pBusinessDate"/>
            <xsl:with-param name="pRows_S" select="$vFirst_Rows_S_Version0_Option"/>
            <xsl:with-param name="pSeqNum" select="1"/>
          </xsl:call-template>
        </xsl:if>
        <xsl:if test="count(msxsl:node-set($vFirst_Rows_S_Version0_Future)/r) > 0">
          <xsl:call-template name="SQLTableDC_Update">
            <xsl:with-param name="pMarket" select="$pMarket"/>
            <xsl:with-param name="pContractSymbol" select="$pContractSymbol"/>
            <xsl:with-param name="pTickSize" select="$pTickSize"/>
            <xsl:with-param name="pTickValue" select="$pTickValue"/>
            <xsl:with-param name="pCategory" select="'F'"/>
            <xsl:with-param name="pVersion" select="'0'"/>
            <xsl:with-param name="pBusinessDate" select="$pBusinessDate"/>
            <xsl:with-param name="pRows_S" select="$vFirst_Rows_S_Version0_Future"/>
            <xsl:with-param name="pSeqNum" select="2"/>
          </xsl:call-template>
        </xsl:if>


        <!-- 20111021 MF RiskData -->
        <!-- Update PARAMSEUREX_CONTRACT table - complete the datas import for group/class elements of the EUREX hierarchy  -->
        <xsl:if test="$vIsRiskDataImport = 'true'">

          <!-- get the right iddc   -->
          <xsl:variable name="vIDDCParameter">
            <xsl:choose>
              <xsl:when test="count(msxsl:node-set($vFirst_Rows_S_Version0_Option)/r) > 0">
                <xsl:value-of select="'parameters.IDDCO'"/>
              </xsl:when>
              <xsl:when test="count(msxsl:node-set($vFirst_Rows_S_Version0_Future)/r) > 0">
                <xsl:value-of select="'parameters.IDDCF'"/>
              </xsl:when>
            </xsl:choose>
          </xsl:variable>

          <!-- update existing  group/class elements with tick size and tick value  -->
          <xsl:call-template name="LIGHT_PARAMSEUREX_CONTRACT_U">
            <xsl:with-param name="pSequenceNumber" select="'2'"/>
            <xsl:with-param name="pIDDC" select="$vIDDCParameter"/>
            <xsl:with-param name="pSym" select="$pContractSymbol"/>
            <xsl:with-param name="pPrdTk" select="$pTickSize"/>
            <xsl:with-param name="pPrdTkV" select="$pTickValue"/>
            <xsl:with-param name="pMgnStl" select="d[@n='MgnStl']/@v"/>
            <xsl:with-param name="pDatetimeRDBMS" select="'parameters.DatetimeRDBMS'"/>
            <xsl:with-param name="pUserId" select="'parameters.UserId'"/>
          </xsl:call-template>

        </xsl:if>

      </r>
    </xsl:if>

  </xsl:template>
  <!--//-->
  <xsl:template name="MultiplierProcess">
    <xsl:param name="pTickSize"/>
    <xsl:param name="pTickValue"/>
    <xsl:param name="pTradingUnit"/>

    <xsl:value-of select="(number($pTickValue) div number($pTickSize)) * number($pTradingUnit)"/>

  </xsl:template>
  <!--//-->
  <xsl:template name="StrikePriceDiv">
    <xsl:param name="pHowManyZero"/>
    <xsl:choose>
      <xsl:when test="number($pHowManyZero) > 0">
        <xsl:variable name="vRecursiveResult">
          <xsl:call-template name="StrikePriceDiv">
            <xsl:with-param name="pHowManyZero" select="number($pHowManyZero) - 1"/>
          </xsl:call-template>
        </xsl:variable>
        <xsl:value-of select="10 * number($vRecursiveResult)"/>
      </xsl:when>
      <xsl:otherwise>
        <xsl:value-of select="1"/>
      </xsl:otherwise>
    </xsl:choose>
  </xsl:template>

  <!-- ================================================== -->
  <!--        SQL query Templates                         -->
  <!-- ================================================== -->
  <xsl:template name="SQLQueryDerivativeContract">
    <xsl:param name="pResultColumn"/>
    <xsl:param name="pMarket"/>
    <xsl:param name="pContractSymbol"/>
    <xsl:param name="pCategory"/>
    <xsl:param name="pVersion"/>


    <!-- BD 20130520 : Appel du template SQLDTENABLEDDTDISABLED pour vérifier la validité du DC sélectionné -->
    <sql cd="select">
      <xsl:attribute name="rt">
        <xsl:value-of select="$pResultColumn"/>
      </xsl:attribute>
      select dc.IDDC, dc.IDM,
      case when dc.ISAUTOSETTING = 1 then dc.ISAUTOSETTING else null end as ISAUTOSETTING,
      case when dc.ISAUTOSETTINGASSET = 1 then dc.ISAUTOSETTINGASSET else null end as ISAUTOSETTINGASSET
      from dbo.DERIVATIVECONTRACT dc
      inner join dbo.MARKET mk on mk.IDM = dc.IDM
      where mk.EXCHANGEACRONYM = @EXACR
      and dc.CONTRACTSYMBOL = @CSYMB
      and dc.CATEGORY = @CAT
      and dc.CONTRACTATTRIBUTE = @CATTR
      and ( ( dc.DTDISABLED is null ) or ( dc.DTDISABLED > @BDT ) )
      <p n="BDT" t="dt">
        <xsl:attribute name="v">
          <xsl:value-of select="/iotask/parameters/parameter[@id='DTBUSINESS']"/>
        </xsl:attribute>
      </p>
      <p n="EXACR" v="{$pMarket}"/>
      <p n="CSYMB" v="{$pContractSymbol}"/>
      <p n="CAT" v="{$pCategory}"/>
      <p n="CATTR" v="{$pVersion}"/>
    </sql>

  </xsl:template>
  <!--//-->
  <xsl:template name="SQLQueryAsset_ETD">
    <xsl:param name="pResultColumn"/>
    <xsl:param name="pMarket"/>
    <xsl:param name="pContractSymbol"/>
    <xsl:param name="pCategory"/>
    <xsl:param name="pVersion"/>
    <xsl:param name="pMaturityMonth"/>
    <xsl:param name="pPutCall"/>
    <xsl:param name="pStrike"/>

    <sql cd="select" rt="IDASSET">
      select distinct a.IDASSET
      from dbo.ASSET_ETD a
      inner join dbo.DERIVATIVEATTRIB da on da.IDDERIVATIVEATTRIB = a.IDDERIVATIVEATTRIB
      inner join dbo.MATURITY ma on ma.IDMATURITY = da.IDMATURITY
      inner join dbo.DERIVATIVECONTRACT dc on dc.IDDC = da.IDDC
      inner join dbo.MARKET mk on mk.IDM = dc.IDM
      inner join dbo.TRADE tr on tr.IDASSET = a.IDASSET
      where mk.EXCHANGEACRONYM = @EXACR
      and dc.CONTRACTSYMBOL = @CSYMB
      and dc.CATEGORY = @CAT
      and dc.CONTRACTATTRIBUTE = @CATTR
      and ma.MATURITYMONTHYEAR = @MMY
      <xsl:if test="string-length($pPutCall) > 0">
        and a.PUTCALL = @PC
        and a.STRIKEPRICE = @STK
      </xsl:if>
      <p n="EXACR" v="{$pMarket}"/>
      <p n="CSYMB" v="{$pContractSymbol}"/>
      <p n="CAT" v="{$pCategory}"/>
      <p n="CATTR" v="{$pVersion}"/>
      <p n="MMY" v="{$pMaturityMonth}"/>
      <xsl:if test="string-length($pPutCall) > 0">
        <p n="PC" v="{$pPutCall}"/>
        <p n="STK" t="dc" v="{$pStrike}"/>
      </xsl:if>
    </sql>
  </xsl:template>

  <!-- ================================================== -->
  <!--        SQL Table Templates                         -->
  <!-- ================================================== -->
  <!-- Mise à jour de la table DERIVATIVECONTRACT:
  - CONTRACTMULTIPLIER et MINPRICEINCRAMOUNT : uniquement pour les contrats avec version = 0
  - MINPRICEINCR : pour tous les contrats
  -->
  <xsl:template name="SQLTableDC_Update">
    <xsl:param name="pMarket"/>
    <xsl:param name="pContractSymbol"/>
    <xsl:param name="pTickSize"/>
    <xsl:param name="pTickValue"/>
    <xsl:param name="pCategory"/>
    <xsl:param name="pVersion"/>
    <xsl:param name="pBusinessDate"/>
    <xsl:param name="pRows_S"/>
    <xsl:param name="pSeqNum"/>

    <xsl:variable name="vTradingUnit">
      <xsl:if test="number($pVersion)=0">
        <xsl:value-of select="msxsl:node-set($pRows_S)/r/d[@n='TrdUnt']/@v"/>
      </xsl:if>
    </xsl:variable>

    <!-- Calculate the multiplier : Tic Value/Tic Size * Trading Unit-->
    <xsl:variable name="vMultiplier">
      <xsl:if test="number($pVersion)=0">
        <xsl:call-template name="MultiplierProcess">
          <xsl:with-param name="pTickSize" select="$pTickSize"/>
          <xsl:with-param name="pTickValue" select="$pTickValue"/>
          <xsl:with-param name="pTradingUnit" select="$vTradingUnit"/>
        </xsl:call-template>
      </xsl:if>
    </xsl:variable>

    <!-- Mise à jours de la table DERIVATIVECONTRACT -->
    <tbl n="DERIVATIVECONTRACT" a="U" sn="{$pSeqNum}">
      <xsl:if test="number($pVersion)=0">
        <!--Pour le recalcul des Events-->
        <mq n="QuotationHandlingMQueue" a="U">
          <p n="TIME" t="d" f="yyyy-MM-dd" v="{$pBusinessDate}"/>
          <p n="IDDC" t="i" v="parameters.IDDC{$pCategory}"/>
          <p n="IsCashFlowsVal" t="b" v="true"/>
        </mq>
      </xsl:if>
      <c n="IDDC" dk="true" t="i" v="parameters.IDDC{$pCategory}">
        <ctls>
          <ctl a="RejectRow" rt="true" lt="None">
            <sl fn="ISNULL()"/>
          </ctl>
          <ctl a="RejectColumn" rt="true" lt="None" v="true"/>
        </ctls>
      </c>
      <c n="ISAUTOSETTING" t="b">
        <xsl:call-template name="SQLQueryDerivativeContract">
          <xsl:with-param name="pResultColumn" select="'ISAUTOSETTING'"/>
          <xsl:with-param name="pMarket" select="$pMarket"/>
          <xsl:with-param name="pContractSymbol" select="$pContractSymbol"/>
          <xsl:with-param name="pCategory" select="$pCategory"/>
          <xsl:with-param name="pVersion" select="$pVersion"/>
        </xsl:call-template>
        <ctls>
          <ctl a="RejectRow" rt="true" lt="None">
            <sl fn="ISNULL()"/>
          </ctl>
          <ctl a="RejectColumn" rt="true" lt="None" v="true"/>
        </ctls>
      </c>

      <!-- Mise à jours de la colonne Tick Size (MINPRICEINCR) pour tous les DC existants-->
      <c n="MINPRICEINCR" dku="true" t="dc" v="{$pTickSize}"/>

      <xsl:if test="number($pVersion)=0">
        <!-- Mise à jours des colonnes CONTRACTMULTIPLIER et Tick value(MINPRICEINCRAMOUNT):
                    - pour tous les DC existants
                    - avec version (CONTRACTATTRIBUTE) = 0 -->
        <c n="CONTRACTMULTIPLIER" dku="true" t="dc" v="{$vMultiplier}"/>
        <c n="MINPRICEINCRAMOUNT" dku="true" t="dc" v="{number($pTickValue) * number($vTradingUnit)}"/>
      </xsl:if>
      <c n="DTUPD" t="dt" v="parameters.DatetimeRDBMS"/>
      <c n="IDAUPD" t="i" v="parameters.UserId"/>
    </tbl>
  </xsl:template>




</xsl:stylesheet>
