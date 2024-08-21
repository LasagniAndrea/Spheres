<?xml version="1.0" encoding="utf-16"?>
<!--
=======================================================================================================================
Summary : CCeG - REPOSITORY
File    : MarketData_CCeG_DerivativeContracts_Map.xsl
=======================================================================================================================
Version: v6.0.0.0    Date: 20170420    Author: FL/PLA
Comment: [23064] - Derivative Contracts: Settled amount behavior for "Physical" delivery
Add pPhysettltamount parameter on SQLTableDERIVATIVECONTRACT template
=======================================================================================================================
FL 20161026 [34191] 
Management of Product-Style 
		        A for Options American style 
		        E for Options European style 
		        F for Futures
=======================================================================================================================
FI 20131205 [19275] 
pContractMultiplierSpecified n'existe plus dans le template SQLTableDERIVATIVECONTRACT
=======================================================================================================================
FL 20130517
Management of New DC - IDEM Stock Dividend Futures (cf TRIM : 33299) in template getIDEM_MaturityRule
=======================================================================================================================
FL 20130515
Management of IDEX : Gestion des nouveaux DC de l'IDEX, ayant pour contract symbol
('Y03FP','Y02FP','Y01FP','Q04FP','Q03FP','Q02FP','Q01FP','M03FP','M02FP','M01FP','D01FP','D02FP')
dans l'initialisation du Shifting et Cascading.
=======================================================================================================================
CC 20130429
File MarketData_Idem_DerivativeContract_Import_Map.xsl renamed to MarketData_CCeG_DerivativeContracts_Map.xsl
=======================================================================================================================
FL 20130415
AGREX : Creation of a new maturity rule "XDMI-AGREX Futures Delivery" 
        on AGREX market segment for the DC with the Symbol "DWHMAD" & "DWHMAW".
        Updating of template : getIDEM_MaturityRule
=======================================================================================================================
PL 20130214 
Management of IDEX/AGREX
=======================================================================================================================
FL/PL 20130124
Add getIDEM_MaturityRule
Replace call SQLTableMATURITYRULE_DERIVATIVECONTRACT by call SQLTableDERIVATIVECONTRACT
=======================================================================================================================
GP 20110523                                           
Exercise Style
=======================================================================================================================
-->

<xsl:stylesheet xmlns:xsl="http://www.w3.org/1999/XSL/Transform" version="1.0">

  <xsl:import href="MarketData_Common_SQL.xsl"/>

  <xsl:output method="xml" omit-xml-declaration="no" encoding="UTF-8" indent="yes" media-type="text/xml; charset=ISO-8859-1"/>

  <!-- Includes-->
  <xsl:include href="MarketData_Common.xsl"/>

  <xsl:variable name="gIDEX_ExchangeSymbol" select="'5'"/>
  <xsl:variable name="gAGREX_ExchangeSymbol" select="'8'"/>

  <!--Main template  -->
  <xsl:template match="/iotask">
    <iotask>
      <xsl:call-template name="IOTaskAtt"/>
      <xsl:apply-templates select="parameters"/>
      <xsl:apply-templates select="iotaskdet"/>
    </iotask>
  </xsl:template>

  <!-- Specific template-->
  <xsl:template match="file">
    <file>
      <xsl:call-template name="IOFileAtt"/>
      <xsl:apply-templates select="row"/>
    </file>
  </xsl:template>

  <xsl:template match="row">
    <row useCache="false">
      <xsl:call-template name="IORowAtt"/>
      <xsl:call-template name="rowStream"/>
    </row>
  </xsl:template>

  <!-- ================================================================== -->
  <!-- Template rowStream: Spécifique à chaque importation de Market Data -->
  <!-- ================================================================== -->
  <xsl:template name="rowStream">
    <xsl:variable name="vDerivativeContractIdentifier" select="$gAutomaticCompute"/>

    <xsl:variable name="vExchangeSymbol" select="normalize-space(data[@name='MI'])"/>

    <xsl:variable name="vDescription" select="normalize-space(data[@name='DE'])"/>

    <!-- F: Future, O: Option -->
    <!-- NB.: C: Equity, CEF, ETF, W: Warrant, V: Convertible pour les autres marchés -->
    <xsl:variable name="vClassType" select="normalize-space(data[@name='CT'])"/>

    <xsl:variable name="vContractSymbol" select="normalize-space(data[@name='SY'])"/>

    <xsl:variable name="vMultiplier" select="normalize-space(data[@name='MU'])"/>

    <!-- ExchangeTradedOption, ExchangeTradedFuture -->
    <xsl:variable name="vInstrumentIdentifier">
      <xsl:call-template name="InstrumentIdentifier">
        <xsl:with-param name="pCategory" select="$vClassType"/>
      </xsl:call-template>
    </xsl:variable>

    <!-- Pour l'IDEM le Factor est toujours égal au Multiplier -->
    <!-- NB. Sur l'IDEM il n’y a pas d’options sur Future pour lesquelles le FACTOR est égale à 1 -->
    <xsl:variable name="vContractFactor" select="$vMultiplier"/>

    <xsl:variable name="vProductType" select="normalize-space(data[@name='PT'])"/>

    <!-- FL 20161025 -->
    <!-- O: Options style, A: Options American style, E: Options European style, F: Future Style -->
    <xsl:variable name="vProductStyle" select="normalize-space(data[@name='PS'])"/>

    <xsl:variable name="vProductOptionFuture">
      <xsl:choose>
        <xsl:when test="$vProductStyle = 'A'">
          <xsl:value-of select="'O'"/>
        </xsl:when>
        <xsl:when test="$vProductStyle = 'E'">
          <xsl:value-of select="'O'"/>
        </xsl:when>
        <xsl:when test="$vProductStyle = 'O'">
          <xsl:value-of select="'O'"/>
        </xsl:when>
        <xsl:when test="$vProductStyle = 'F'">
          <xsl:value-of select="'F'"/>
        </xsl:when>
        <xsl:otherwise>
          <xsl:value-of select="$vProductStyle"/>
        </xsl:otherwise>
      </xsl:choose>
    </xsl:variable>

    <xsl:variable name="vFutValuationMethod">
      <xsl:call-template name="GetFutValuationMethod">
        <!-- <xsl:with-param name="pProductStyle" select="$vProductStyle"/> -->
        <xsl:with-param name="pProductStyle" select="$vProductOptionFuture"/>
        <xsl:with-param name="pClassType" select="$vClassType"/>
      </xsl:call-template>
    </xsl:variable>

    <xsl:variable name="vMinPriceIncr">
      <xsl:choose>
        <xsl:when test="$vExchangeSymbol = $gIDEX_ExchangeSymbol">
          <!-- Pour l'IDEX toujours 0.01 -->
          <xsl:value-of select="0.01"/>
        </xsl:when>
        <xsl:when test="$vExchangeSymbol = $gAGREX_ExchangeSymbol">
          <!-- Pour l'AGREX 0.25 -->
          <xsl:value-of select="0.25"/>
        </xsl:when>
        <xsl:otherwise>
          <xsl:value-of select="$gNull"/>
        </xsl:otherwise>
      </xsl:choose>
    </xsl:variable>
    <xsl:variable name="vMinPriceIncrAmount">
      <xsl:choose>
        <xsl:when test="$vExchangeSymbol = $gIDEX_ExchangeSymbol">
          <!-- Pour l'IDEX toujours Multiplier * 0.01 -->
          <xsl:value-of select="0.01 * number($vMultiplier)"/>
        </xsl:when>
        <xsl:when test="$vExchangeSymbol = $gAGREX_ExchangeSymbol">
          <!-- Pour l'AGREX Multiplier * 0.25 -->
          <xsl:value-of select="0.25 * number($vMultiplier)"/>
        </xsl:when>
        <xsl:otherwise>
          <xsl:value-of select="$gNull"/>
        </xsl:otherwise>
      </xsl:choose>
    </xsl:variable>

    <xsl:variable name="vUnitOfMeasure">
      <xsl:choose>
        <xsl:when test="$vExchangeSymbol = $gIDEX_ExchangeSymbol">
          <!-- Pour l'IDEX toujours MWh (MegawattHours) -->
          <xsl:value-of select="'MWh'"/>
        </xsl:when>
        <xsl:when test="$vExchangeSymbol = $gAGREX_ExchangeSymbol">
          <!-- Pour l'AGREX toujours t (MetricTons) -->
          <xsl:value-of select="'t'"/>
        </xsl:when>
        <xsl:otherwise>
          <xsl:value-of select="$gNull"/>
        </xsl:otherwise>
      </xsl:choose>
    </xsl:variable>
    <xsl:variable name="vUnitOfMeasureQty">
      <xsl:choose>
        <xsl:when test="$vExchangeSymbol = $gIDEX_ExchangeSymbol">
          <!-- Pour l'IDEX toujours 1 MWh (1 MegawattHours) -->
          <xsl:value-of select="1"/>
        </xsl:when>
        <xsl:when test="$vExchangeSymbol = $gAGREX_ExchangeSymbol">
          <!-- Pour l'AGREX toujours 50 t (50 MetricTons) -->
          <xsl:value-of select="50"/>
        </xsl:when>
        <xsl:otherwise>
          <xsl:value-of select="$gNull"/>
        </xsl:otherwise>
      </xsl:choose>
    </xsl:variable>

    <xsl:variable name="vContractSymbol_Shift">
      <xsl:choose>
        <xsl:when test="$vExchangeSymbol = $gIDEX_ExchangeSymbol">
          <!-- IDEX: Shifting -->
          <xsl:choose>
            <xsl:when test="$vContractSymbol = 'Y03FB'">
              <xsl:value-of select="'Y02FB'"/>
            </xsl:when>
            <xsl:when test="$vContractSymbol = 'Y02FB'">
              <xsl:value-of select="'Y01FB'"/>
            </xsl:when>
            <xsl:when test="$vContractSymbol = 'Q04FB'">
              <xsl:value-of select="'Q03FB'"/>
            </xsl:when>
            <xsl:when test="$vContractSymbol = 'Q03FB'">
              <xsl:value-of select="'Q02FB'"/>
            </xsl:when>
            <xsl:when test="$vContractSymbol = 'Q02FB'">
              <xsl:value-of select="'Q01FB'"/>
            </xsl:when>
            <xsl:when test="$vContractSymbol = 'M03FB'">
              <xsl:value-of select="'M02FB'"/>
            </xsl:when>
            <xsl:when test="$vContractSymbol = 'M02FB'">
              <xsl:value-of select="'M01FB'"/>
            </xsl:when>
            <xsl:when test="$vContractSymbol = 'M01FB'">
              <xsl:value-of select="'D01FB'"/>
            </xsl:when>
            <xsl:when test="$vContractSymbol = 'D01FB'">
              <!-- Warning, in the CC&G documentation D02FB is called S01FB -->
              <xsl:value-of select="'D02FB'"/>
            </xsl:when>
            <!-- FL 20130515 Pour Shifting Gestion des Nouveaux DC de l'IDEX -->
            <xsl:when test="$vContractSymbol = 'Y03FP'">
              <xsl:value-of select="'Y02FP'"/>
            </xsl:when>
            <xsl:when test="$vContractSymbol = 'Y02FP'">
              <xsl:value-of select="'Y01FP'"/>
            </xsl:when>
            <xsl:when test="$vContractSymbol = 'Q04FP'">
              <xsl:value-of select="'Q03FP'"/>
            </xsl:when>
            <xsl:when test="$vContractSymbol = 'Q03FP'">
              <xsl:value-of select="'Q02FP'"/>
            </xsl:when>
            <xsl:when test="$vContractSymbol = 'Q02FP'">
              <xsl:value-of select="'Q01FP'"/>
            </xsl:when>
            <xsl:when test="$vContractSymbol = 'M03FP'">
              <xsl:value-of select="'M02FP'"/>
            </xsl:when>
            <xsl:when test="$vContractSymbol = 'M02FP'">
              <xsl:value-of select="'M01FP'"/>
            </xsl:when>
            <xsl:when test="$vContractSymbol = 'M01FP'">
              <xsl:value-of select="'D01FP'"/>
            </xsl:when>
            <xsl:when test="$vContractSymbol = 'D01FP'">
              <xsl:value-of select="'D02FP'"/>
            </xsl:when>
            <xsl:otherwise>
              <xsl:value-of select="''"/>
            </xsl:otherwise>
          </xsl:choose>
        </xsl:when>
        <xsl:otherwise>
          <xsl:value-of select="''"/>
        </xsl:otherwise>
      </xsl:choose>
    </xsl:variable>

    <!-- vUnderlyingGroup -->
    <xsl:variable name="vUnderlyingGroup">
      <!-- PL 20130213 IDEX -->
      <xsl:choose>
        <xsl:when test="$vExchangeSymbol = $gIDEX_ExchangeSymbol or $vExchangeSymbol = $gAGREX_ExchangeSymbol">
          <!-- C: Commodities -->
          <xsl:value-of select="'C'"/>
        </xsl:when>
        <xsl:otherwise>
          <xsl:call-template name="GetUnderlyingGroup">
            <xsl:with-param name="pProductType" select="$vProductType"/>
          </xsl:call-template>
        </xsl:otherwise>
      </xsl:choose>
    </xsl:variable>

    <!-- vUnderlyingAsset -->
    <!-- Values from the file (vProductType): E: Equity; I: Index, B: Bond, S: Securities -->
    <!-- Return Values from Template(vUnderlyingAsset) : FS: Equity, FI: Index, FD: Bond, FD: Securities  -->
    <xsl:variable name="vUnderlyingAsset">
      <!-- PL 20130213 IDEX -->
      <xsl:call-template name="getUnderlyingAsset">
        <xsl:with-param name="pExchangeSymbol">
          <xsl:value-of select="$vExchangeSymbol"/>
        </xsl:with-param>
        <xsl:with-param name="pProductType">
          <xsl:value-of select="$vProductType"/>
        </xsl:with-param>
      </xsl:call-template>
    </xsl:variable>

    <!-- Ex.: Symbol UNI1, UnderlyingCode UNI-->
    <xsl:variable name="vUnderlyingCode">
      <xsl:choose>
        <xsl:when test="$vExchangeSymbol = $gIDEX_ExchangeSymbol">
          <!-- IDEX: le fichier ClassFile ne contient pas l'information. On considère 'EL'.  -->
          <xsl:value-of select="'EL'"/>
        </xsl:when>
        <xsl:otherwise>
          <xsl:value-of select="normalize-space(data[@name='UC'])"/>
        </xsl:otherwise>
      </xsl:choose>
    </xsl:variable>

    <xsl:variable name="vUnderlyingIsinCode" select="normalize-space(data[@name='UI'])"/>

    <!-- The exercise style is not available in IDEM files -->
    <!-- We use the following rule: -->
    <!-- 1. If vUnderlyingAsset='FI' (Indices) then Europeen style  (http://www.borsaitaliana.it/derivati/specifichecontrattuali/ftsemiboptions.htm)    -->
    <!-- 2. Otherwise (Equity options)         then American style  (http://www.borsaitaliana.it/derivati/specifichecontrattuali/opzioni-su-azioni.htm) -->
    <!-- NB: vUnderlyingAsset is derived from ProductType (eg. ProductType='I' for vUnderlyingAsset='FI' -->

    <!-- FL 20161026 Now the exercise style is available in IDEM files in ProductStyle  -->
    <!--    For Option:                                                                 -->
    <!--         A for Options American style                                           -->
    <!--         E for Options European style                                           -->
    <xsl:variable name="vExerciseStyle">
      <xsl:choose>
        <xsl:when test="$vClassType='O'">
          <xsl:choose>
            <!-- Index Options -->
            <xsl:when test="$vUnderlyingAsset='FI'">
              <xsl:value-of select="'0'"/>
            </xsl:when>
            <!-- Equity options -->
            <xsl:otherwise>
              <xsl:choose>
                <!-- ProductStyle: E for Options European style  -->
                <xsl:when test="$vProductStyle='E'">
                  <xsl:value-of select="'0'"/>
                </xsl:when>
                <!-- ProductStyle: A for Options American style  -->
                <xsl:when test="$vProductStyle='A'">
                  <xsl:value-of select="'1'"/>
                </xsl:when>
                <xsl:otherwise>
                  <xsl:value-of select="'1'"/>
                </xsl:otherwise>
              </xsl:choose>
            </xsl:otherwise>
          </xsl:choose>
        </xsl:when>
        <xsl:otherwise/>
      </xsl:choose>
    </xsl:variable>

    <!-- =================================================================== -->
    <!--                              Currency                               -->
    <!-- We call the template because in the import file there is EU for EUR -->
    <!-- =================================================================== -->
    <xsl:variable name="vCurrency">
      <xsl:call-template name="GetCurrency">
        <xsl:with-param name="pCurrency" select="normalize-space(data[@name='CU'])"/>
      </xsl:call-template>
    </xsl:variable>

    <!-- Settlement Method                    -->
    <!-- Data non available in the IDEM files -->
    <xsl:variable name="vSettltMethod">
      <!-- PL 20130213 IDEX -->
      <xsl:choose>
        <xsl:when test="$vExchangeSymbol = $gIDEX_ExchangeSymbol">
          <!-- C: Cash-Settlement -->
          <xsl:value-of select="'C'"/>
        </xsl:when>
        <xsl:when test="$vExchangeSymbol = $gAGREX_ExchangeSymbol">
          <!-- P: Physical -->
          <xsl:value-of select="'P'"/>
        </xsl:when>
        <xsl:otherwise>
          <xsl:call-template name="GetSettltMethod">
            <xsl:with-param name="pProductType" select="$vProductType"/>
            <xsl:with-param name="pContractSymbol" select="$vContractSymbol"/>
          </xsl:call-template>
        </xsl:otherwise>
      </xsl:choose>
    </xsl:variable>

    <!-- Asset Category -->
    <xsl:variable name="vAssetCategory">
      <!-- PL 20130218 IDEX -->
      <xsl:choose>
        <xsl:when test="$vExchangeSymbol = $gIDEX_ExchangeSymbol or $vExchangeSymbol = $gAGREX_ExchangeSymbol">
          <!-- Commodities -->
          <xsl:value-of select="'Commodity'"/>
        </xsl:when>
        <xsl:otherwise>
          <xsl:call-template name="GetAssetCategory">
            <xsl:with-param name="pProductType" select="$vProductType"/>
          </xsl:call-template>
        </xsl:otherwise>
      </xsl:choose>
    </xsl:variable>

    <xsl:variable name="vUnderlyingISO10383">
      <!-- PL 20130218 IDEX -->
      <xsl:choose>
        <xsl:when test="$vExchangeSymbol = $gIDEX_ExchangeSymbol">
          <!-- IPEX -->
          <xsl:value-of select="'XGME'"/>
        </xsl:when>
        <xsl:otherwise>
          <xsl:value-of select="$gNull"/>
        </xsl:otherwise>
      </xsl:choose>
    </xsl:variable>

    <!-- Maturity Rule -->
    <xsl:variable name="vMaturityRule">
      <!-- FL/PL 20130124 New variable -->
      <xsl:call-template name="getIDEM_MaturityRule">
        <xsl:with-param name="pContractSymbol" select="$vContractSymbol"/>
        <xsl:with-param name="pUnderlyingAsset" select="$vUnderlyingAsset"/>
        <xsl:with-param name="pCategory" select="$vClassType"/>
      </xsl:call-template>
    </xsl:variable>

    <!-- Additional category filter to identify contracts -->
    <!-- Sur IDEM/IDEX 2 Derivative Contract portant sur le même sous-jacent ont le même symbol -->
    <!-- Ex. future sur actions ACE et option sur actions ACE. Le symbol pour les deux Derivative Contract est ACE -->
    <!-- Pour vérifier si un Derivative Contract existe déjà dans la table il faut ajouter une condition sur la catégorie (F/O) -->
    <!-- Autrement, une fois inséré le future (option) ACE, il sera impossible d'insérer l'option (le future) ACE     -->
    <!-- car la requête ne considère que le symbol (et le marché) pour vérifier si le Derivative Contract existe déjà -->
    <xsl:variable name="vExtSQLFilterValues" select="concat ('KEY_OP',',',$vClassType)"/>
    <xsl:variable name="vExtSQLFilterNames"  select="concat ($gIdemSpecial,',','CATEGORY')"/>

    <!-- BD 20130604 : vFinalSettltPrice -->
    <xsl:variable name="vFinalSettltPrice">
      <xsl:choose>
        <xsl:when test="($vClassType = 'O') and ($vAssetCategory = $gEquityAsset)">LastTradingDay</xsl:when>
        <xsl:otherwise>ExpiryDate</xsl:otherwise>
      </xsl:choose>
    </xsl:variable>

    <!-- FL/PLA 20170420 [23064] add column PHYSETTLTAMOUNT -->
    <xsl:variable name="vPhysettltamount">
      <xsl:choose>
        <xsl:when test="($vSettltMethod = 'C')">NA</xsl:when>
        <xsl:otherwise>None</xsl:otherwise>
      </xsl:choose>
    </xsl:variable>

    <!-- FL/PL 20130124 Use SQLTableDERIVATIVECONTRACT instead of SQLTableMATURITYRULE_DERIVATIVECONTRACT -->
    <!-- PL 20130212 Add pISO10383 -->
    <xsl:call-template name="SQLTableDERIVATIVECONTRACT">
      <xsl:with-param name="pISO10383" select="'XDMI'"/>
      <xsl:with-param name="pExchangeSymbol" select="$vExchangeSymbol"/>
      <xsl:with-param name="pMaturityRuleIdentifier" select="$vMaturityRule"/>
      <xsl:with-param name="pContractSymbol" select="$vContractSymbol"/>
      <xsl:with-param name="pDerivativeContractIdentifier" select="$vDerivativeContractIdentifier"/>
      <xsl:with-param name="pContractDisplayName" select="$vDescription"/>
      <xsl:with-param name="pInstrumentIdentifier" select="$vInstrumentIdentifier"/>
      <xsl:with-param name="pCurrency" select="$vCurrency"/>
      <xsl:with-param name="pCategory" select="$vClassType"/>
      <xsl:with-param name="pExerciseStyle" select="$vExerciseStyle"/>
      <xsl:with-param name="pSettlMethod" select="$vSettltMethod"/>
      <xsl:with-param name="pPhysettltamount" select="$vPhysettltamount"/>
      <xsl:with-param name="pFutValuationMethod" select="$vFutValuationMethod"/>
      <xsl:with-param name="pMinPriceIncr" select="$vMinPriceIncr"/>
      <xsl:with-param name="pMinPriceIncrAmount" select="$vMinPriceIncrAmount"/>
      <xsl:with-param name="pContractFactor" select="$vContractFactor"/>
      <xsl:with-param name="pContractMultiplier" select="$vMultiplier"/>
      <!-- FI 20131205 [19275] pContractMultiplierSpecified n'existe plus -->
      <!--<xsl:with-param name="pContractMultiplierSpecified" select="true()"/>-->

      <xsl:with-param name="pUnitOfMeasure" select="$vUnitOfMeasure"/>
      <xsl:with-param name="pUnitOfMeasureQty" select="$vUnitOfMeasureQty"/>

      <xsl:with-param name="pUnderlyingGroup" select="$vUnderlyingGroup"/>
      <xsl:with-param name="pUnderlyingAsset" select="$vUnderlyingAsset"/>

      <xsl:with-param name="pContractSymbol_Shift" select="$vContractSymbol_Shift"/>

      <xsl:with-param name="pAssetCategory" select="$vAssetCategory"/>
      <xsl:with-param name="pUnderlyingIdentifier" select="$vUnderlyingCode"/>
      <xsl:with-param name="pUnderlyingISO10383" select="$vUnderlyingISO10383"/>
      <xsl:with-param name="pUnderlyingIsinCode" select="$vUnderlyingIsinCode"/>
      <!-- BD 20130513 pDerivativeContractIsAutoSetting=gTrue -->
      <xsl:with-param name="pDerivativeContractIsAutoSetting" select="$gTrue"/>
      <!-- BD 20130521 ISAUTOSETTINGASSET=1 pour les DC du CCeG -->
      <xsl:with-param name="pIsAutoSettingAsset" select="$gTrue"/>
      <xsl:with-param name="pInsertMaturityRule" select="$gFalse"/>
      <xsl:with-param name="pExtSQLFilterValues" select="$vExtSQLFilterValues"/>
      <xsl:with-param name="pExtSQLFilterNames"  select="$vExtSQLFilterNames"/>

      <!-- Ce paramètre n’est jamais valorisé car il n’y a pas d’option sur Futures sur l’IDEM/IDEX AGREX  -->
      <xsl:with-param name="pUnderlyingContractSymbol"/>

      <!-- Paramètres non disponibles dans le fichier IDEM/IDEX files -->
      <xsl:with-param name="pNominalValue"/>
      <xsl:with-param name="pAssignmentMethod"/>
      <xsl:with-param name="pContractAttribute"/>
      <xsl:with-param name="pStrikeDecLocator"/>

      <!-- BD 20130604 -->
      <xsl:with-param name="pFinalSettltPrice" select="$vFinalSettltPrice"/>

    </xsl:call-template>

    <!-- Cascading Y01FB -->
    <xsl:if test="$vContractSymbol = 'Y01FB'">
      <xsl:call-template name="Cascading">
        <xsl:with-param name="pSequenceno" select="3"/>
        <xsl:with-param name="pContractSymbol" select="$vContractSymbol"/>
        <xsl:with-param name="pMATURITYMONTH" select="12"/>
        <xsl:with-param name="pCASCContractSymbol" select="'Q04FB'"/>
        <xsl:with-param name="pCASCMATURITYMONTH" select="12"/>
      </xsl:call-template>
      <xsl:call-template name="Cascading">
        <xsl:with-param name="pSequenceno" select="4"/>
        <xsl:with-param name="pContractSymbol" select="$vContractSymbol"/>
        <xsl:with-param name="pMATURITYMONTH" select="12"/>
        <xsl:with-param name="pCASCContractSymbol" select="'Q03FB'"/>
        <xsl:with-param name="pCASCMATURITYMONTH" select="9"/>
      </xsl:call-template>
      <xsl:call-template name="Cascading">
        <xsl:with-param name="pSequenceno" select="5"/>
        <xsl:with-param name="pContractSymbol" select="$vContractSymbol"/>
        <xsl:with-param name="pMATURITYMONTH" select="12"/>
        <xsl:with-param name="pCASCContractSymbol" select="'Q02FB'"/>
        <xsl:with-param name="pCASCMATURITYMONTH" select="6"/>
      </xsl:call-template>
      <xsl:call-template name="Cascading">
        <xsl:with-param name="pSequenceno" select="6"/>
        <xsl:with-param name="pContractSymbol" select="$vContractSymbol"/>
        <xsl:with-param name="pMATURITYMONTH" select="12"/>
        <xsl:with-param name="pCASCContractSymbol" select="'M03FB'"/>
        <xsl:with-param name="pCASCMATURITYMONTH" select="3"/>
      </xsl:call-template>
      <xsl:call-template name="Cascading">
        <xsl:with-param name="pSequenceno" select="7"/>
        <xsl:with-param name="pContractSymbol" select="$vContractSymbol"/>
        <xsl:with-param name="pMATURITYMONTH" select="12"/>
        <xsl:with-param name="pCASCContractSymbol" select="'M02FB'"/>
        <xsl:with-param name="pCASCMATURITYMONTH" select="2"/>
      </xsl:call-template>
      <xsl:call-template name="Cascading">
        <xsl:with-param name="pSequenceno" select="8"/>
        <xsl:with-param name="pContractSymbol" select="$vContractSymbol"/>
        <xsl:with-param name="pMATURITYMONTH" select="12"/>
        <xsl:with-param name="pCASCContractSymbol" select="'M01FB'"/>
        <xsl:with-param name="pCASCMATURITYMONTH" select="1"/>
      </xsl:call-template>
    </xsl:if>

    <!-- Cascading Q01FB -->
    <xsl:if test="$vContractSymbol = 'Q01FB'">
      <xsl:call-template name="Cascading">
        <xsl:with-param name="pSequenceno" select="9"/>
        <xsl:with-param name="pContractSymbol" select="$vContractSymbol"/>
        <xsl:with-param name="pMATURITYMONTH" select="12"/>
        <xsl:with-param name="pCASCContractSymbol" select="'M03FB'"/>
        <xsl:with-param name="pCASCMATURITYMONTH" select="12"/>
      </xsl:call-template>
      <xsl:call-template name="Cascading">
        <xsl:with-param name="pSequenceno" select="10"/>
        <xsl:with-param name="pContractSymbol" select="$vContractSymbol"/>
        <xsl:with-param name="pMATURITYMONTH" select="12"/>
        <xsl:with-param name="pCASCContractSymbol" select="'M02FB'"/>
        <xsl:with-param name="pCASCMATURITYMONTH" select="11"/>
      </xsl:call-template>
      <xsl:call-template name="Cascading">
        <xsl:with-param name="pSequenceno" select="11"/>
        <xsl:with-param name="pContractSymbol" select="$vContractSymbol"/>
        <xsl:with-param name="pMATURITYMONTH" select="12"/>
        <xsl:with-param name="pCASCContractSymbol" select="'M01FB'"/>
        <xsl:with-param name="pCASCMATURITYMONTH" select="10"/>
      </xsl:call-template>

      <xsl:call-template name="Cascading">
        <xsl:with-param name="pSequenceno" select="12"/>
        <xsl:with-param name="pContractSymbol" select="$vContractSymbol"/>
        <xsl:with-param name="pMATURITYMONTH" select="9"/>
        <xsl:with-param name="pCASCContractSymbol" select="'M03FB'"/>
        <xsl:with-param name="pCASCMATURITYMONTH" select="9"/>
      </xsl:call-template>
      <xsl:call-template name="Cascading">
        <xsl:with-param name="pSequenceno" select="13"/>
        <xsl:with-param name="pContractSymbol" select="$vContractSymbol"/>
        <xsl:with-param name="pMATURITYMONTH" select="9"/>
        <xsl:with-param name="pCASCContractSymbol" select="'M02FB'"/>
        <xsl:with-param name="pCASCMATURITYMONTH" select="8"/>
      </xsl:call-template>
      <xsl:call-template name="Cascading">
        <xsl:with-param name="pSequenceno" select="14"/>
        <xsl:with-param name="pContractSymbol" select="$vContractSymbol"/>
        <xsl:with-param name="pMATURITYMONTH" select="9"/>
        <xsl:with-param name="pCASCContractSymbol" select="'M01FB'"/>
        <xsl:with-param name="pCASCMATURITYMONTH" select="7"/>
      </xsl:call-template>

      <xsl:call-template name="Cascading">
        <xsl:with-param name="pSequenceno" select="15"/>
        <xsl:with-param name="pContractSymbol" select="$vContractSymbol"/>
        <xsl:with-param name="pMATURITYMONTH" select="6"/>
        <xsl:with-param name="pCASCContractSymbol" select="'M03FB'"/>
        <xsl:with-param name="pCASCMATURITYMONTH" select="6"/>
      </xsl:call-template>
      <xsl:call-template name="Cascading">
        <xsl:with-param name="pSequenceno" select="16"/>
        <xsl:with-param name="pContractSymbol" select="$vContractSymbol"/>
        <xsl:with-param name="pMATURITYMONTH" select="6"/>
        <xsl:with-param name="pCASCContractSymbol" select="'M02FB'"/>
        <xsl:with-param name="pCASCMATURITYMONTH" select="5"/>
      </xsl:call-template>
      <xsl:call-template name="Cascading">
        <xsl:with-param name="pSequenceno" select="17"/>
        <xsl:with-param name="pContractSymbol" select="$vContractSymbol"/>
        <xsl:with-param name="pMATURITYMONTH" select="6"/>
        <xsl:with-param name="pCASCContractSymbol" select="'M01FB'"/>
        <xsl:with-param name="pCASCMATURITYMONTH" select="4"/>
      </xsl:call-template>

      <xsl:call-template name="Cascading">
        <xsl:with-param name="pSequenceno" select="18"/>
        <xsl:with-param name="pContractSymbol" select="$vContractSymbol"/>
        <xsl:with-param name="pMATURITYMONTH" select="3"/>
        <xsl:with-param name="pCASCContractSymbol" select="'M03FB'"/>
        <xsl:with-param name="pCASCMATURITYMONTH" select="3"/>
      </xsl:call-template>
      <xsl:call-template name="Cascading">
        <xsl:with-param name="pSequenceno" select="19"/>
        <xsl:with-param name="pContractSymbol" select="$vContractSymbol"/>
        <xsl:with-param name="pMATURITYMONTH" select="3"/>
        <xsl:with-param name="pCASCContractSymbol" select="'M02FB'"/>
        <xsl:with-param name="pCASCMATURITYMONTH" select="2"/>
      </xsl:call-template>
      <xsl:call-template name="Cascading">
        <xsl:with-param name="pSequenceno" select="20"/>
        <xsl:with-param name="pContractSymbol" select="$vContractSymbol"/>
        <xsl:with-param name="pMATURITYMONTH" select="3"/>
        <xsl:with-param name="pCASCContractSymbol" select="'M01FB'"/>
        <xsl:with-param name="pCASCMATURITYMONTH" select="1"/>
      </xsl:call-template>
    </xsl:if>

    <!-- FL 20130515 Pour Cascading Gestion des Nouveaux DC de l'IDEX (Y01FP & Q01FP) -->
    <!-- Cascading Y01FP -->
    <xsl:if test="$vContractSymbol = 'Y01FP'">
      <xsl:call-template name="Cascading">
        <xsl:with-param name="pSequenceno" select="3"/>
        <xsl:with-param name="pContractSymbol" select="$vContractSymbol"/>
        <xsl:with-param name="pMATURITYMONTH" select="12"/>
        <xsl:with-param name="pCASCContractSymbol" select="'Q04FP'"/>
        <xsl:with-param name="pCASCMATURITYMONTH" select="12"/>
      </xsl:call-template>
      <xsl:call-template name="Cascading">
        <xsl:with-param name="pSequenceno" select="4"/>
        <xsl:with-param name="pContractSymbol" select="$vContractSymbol"/>
        <xsl:with-param name="pMATURITYMONTH" select="12"/>
        <xsl:with-param name="pCASCContractSymbol" select="'Q03FP'"/>
        <xsl:with-param name="pCASCMATURITYMONTH" select="9"/>
      </xsl:call-template>
      <xsl:call-template name="Cascading">
        <xsl:with-param name="pSequenceno" select="5"/>
        <xsl:with-param name="pContractSymbol" select="$vContractSymbol"/>
        <xsl:with-param name="pMATURITYMONTH" select="12"/>
        <xsl:with-param name="pCASCContractSymbol" select="'Q02FP'"/>
        <xsl:with-param name="pCASCMATURITYMONTH" select="6"/>
      </xsl:call-template>
      <xsl:call-template name="Cascading">
        <xsl:with-param name="pSequenceno" select="6"/>
        <xsl:with-param name="pContractSymbol" select="$vContractSymbol"/>
        <xsl:with-param name="pMATURITYMONTH" select="12"/>
        <xsl:with-param name="pCASCContractSymbol" select="'M03FP'"/>
        <xsl:with-param name="pCASCMATURITYMONTH" select="3"/>
      </xsl:call-template>
      <xsl:call-template name="Cascading">
        <xsl:with-param name="pSequenceno" select="7"/>
        <xsl:with-param name="pContractSymbol" select="$vContractSymbol"/>
        <xsl:with-param name="pMATURITYMONTH" select="12"/>
        <xsl:with-param name="pCASCContractSymbol" select="'M02FP'"/>
        <xsl:with-param name="pCASCMATURITYMONTH" select="2"/>
      </xsl:call-template>
      <xsl:call-template name="Cascading">
        <xsl:with-param name="pSequenceno" select="8"/>
        <xsl:with-param name="pContractSymbol" select="$vContractSymbol"/>
        <xsl:with-param name="pMATURITYMONTH" select="12"/>
        <xsl:with-param name="pCASCContractSymbol" select="'M01FP'"/>
        <xsl:with-param name="pCASCMATURITYMONTH" select="1"/>
      </xsl:call-template>
    </xsl:if>

    <!-- Cascading Q01FP -->
    <xsl:if test="$vContractSymbol = 'Q01FP'">
      <xsl:call-template name="Cascading">
        <xsl:with-param name="pSequenceno" select="9"/>
        <xsl:with-param name="pContractSymbol" select="$vContractSymbol"/>
        <xsl:with-param name="pMATURITYMONTH" select="12"/>
        <xsl:with-param name="pCASCContractSymbol" select="'M03FP'"/>
        <xsl:with-param name="pCASCMATURITYMONTH" select="12"/>
      </xsl:call-template>
      <xsl:call-template name="Cascading">
        <xsl:with-param name="pSequenceno" select="10"/>
        <xsl:with-param name="pContractSymbol" select="$vContractSymbol"/>
        <xsl:with-param name="pMATURITYMONTH" select="12"/>
        <xsl:with-param name="pCASCContractSymbol" select="'M02FP'"/>
        <xsl:with-param name="pCASCMATURITYMONTH" select="11"/>
      </xsl:call-template>
      <xsl:call-template name="Cascading">
        <xsl:with-param name="pSequenceno" select="11"/>
        <xsl:with-param name="pContractSymbol" select="$vContractSymbol"/>
        <xsl:with-param name="pMATURITYMONTH" select="12"/>
        <xsl:with-param name="pCASCContractSymbol" select="'M01FP'"/>
        <xsl:with-param name="pCASCMATURITYMONTH" select="10"/>
      </xsl:call-template>

      <xsl:call-template name="Cascading">
        <xsl:with-param name="pSequenceno" select="12"/>
        <xsl:with-param name="pContractSymbol" select="$vContractSymbol"/>
        <xsl:with-param name="pMATURITYMONTH" select="9"/>
        <xsl:with-param name="pCASCContractSymbol" select="'M03FP'"/>
        <xsl:with-param name="pCASCMATURITYMONTH" select="9"/>
      </xsl:call-template>
      <xsl:call-template name="Cascading">
        <xsl:with-param name="pSequenceno" select="13"/>
        <xsl:with-param name="pContractSymbol" select="$vContractSymbol"/>
        <xsl:with-param name="pMATURITYMONTH" select="9"/>
        <xsl:with-param name="pCASCContractSymbol" select="'M02FP'"/>
        <xsl:with-param name="pCASCMATURITYMONTH" select="8"/>
      </xsl:call-template>
      <xsl:call-template name="Cascading">
        <xsl:with-param name="pSequenceno" select="14"/>
        <xsl:with-param name="pContractSymbol" select="$vContractSymbol"/>
        <xsl:with-param name="pMATURITYMONTH" select="9"/>
        <xsl:with-param name="pCASCContractSymbol" select="'M01FP'"/>
        <xsl:with-param name="pCASCMATURITYMONTH" select="7"/>
      </xsl:call-template>

      <xsl:call-template name="Cascading">
        <xsl:with-param name="pSequenceno" select="15"/>
        <xsl:with-param name="pContractSymbol" select="$vContractSymbol"/>
        <xsl:with-param name="pMATURITYMONTH" select="6"/>
        <xsl:with-param name="pCASCContractSymbol" select="'M03FP'"/>
        <xsl:with-param name="pCASCMATURITYMONTH" select="6"/>
      </xsl:call-template>
      <xsl:call-template name="Cascading">
        <xsl:with-param name="pSequenceno" select="16"/>
        <xsl:with-param name="pContractSymbol" select="$vContractSymbol"/>
        <xsl:with-param name="pMATURITYMONTH" select="6"/>
        <xsl:with-param name="pCASCContractSymbol" select="'M02FP'"/>
        <xsl:with-param name="pCASCMATURITYMONTH" select="5"/>
      </xsl:call-template>
      <xsl:call-template name="Cascading">
        <xsl:with-param name="pSequenceno" select="17"/>
        <xsl:with-param name="pContractSymbol" select="$vContractSymbol"/>
        <xsl:with-param name="pMATURITYMONTH" select="6"/>
        <xsl:with-param name="pCASCContractSymbol" select="'M01FP'"/>
        <xsl:with-param name="pCASCMATURITYMONTH" select="4"/>
      </xsl:call-template>

      <xsl:call-template name="Cascading">
        <xsl:with-param name="pSequenceno" select="18"/>
        <xsl:with-param name="pContractSymbol" select="$vContractSymbol"/>
        <xsl:with-param name="pMATURITYMONTH" select="3"/>
        <xsl:with-param name="pCASCContractSymbol" select="'M03FP'"/>
        <xsl:with-param name="pCASCMATURITYMONTH" select="3"/>
      </xsl:call-template>
      <xsl:call-template name="Cascading">
        <xsl:with-param name="pSequenceno" select="19"/>
        <xsl:with-param name="pContractSymbol" select="$vContractSymbol"/>
        <xsl:with-param name="pMATURITYMONTH" select="3"/>
        <xsl:with-param name="pCASCContractSymbol" select="'M02FP'"/>
        <xsl:with-param name="pCASCMATURITYMONTH" select="2"/>
      </xsl:call-template>
      <xsl:call-template name="Cascading">
        <xsl:with-param name="pSequenceno" select="20"/>
        <xsl:with-param name="pContractSymbol" select="$vContractSymbol"/>
        <xsl:with-param name="pMATURITYMONTH" select="3"/>
        <xsl:with-param name="pCASCContractSymbol" select="'M01FP'"/>
        <xsl:with-param name="pCASCMATURITYMONTH" select="1"/>
      </xsl:call-template>
    </xsl:if>

  </xsl:template>

  <!-- *************************************************************************************************** -->
  <!-- getIDEM_MaturityRule - return IDEM MaturityRule Identifier from an IDEM Derivative Contract         -->
  <!-- *************************************************************************************************** -->
  <!-- Affectation des règles d'échéances sur l'IDEM sur la base de la matrice suivante:
       =======================================================================================================
       pUnderlyingAsset         pCategory    pContractSymbol   Règle d'échéance retournée
       =======================================================================================================
       FS: Stock-Equities       F: Future                      XDMI Equity Futures
       FS: Stock-Equities       O: Options                     XDMI Equity Otptions
       
       FI: Indices              F: Future    FDIV              XDMI Dividend Futures
       FI: Indices              F: Future    1...              XDMI Dividend Futures
       FI: Indices              F: Future                      XDMI FTSE MIB Futures
       
       FI: Indices              O: Options   MIBO1W            XDMI FTSE MIB Options Weekly 1 (MIBO1W)
       FI: Indices              O: Options   MIBO2W            XDMI FTSE MIB Options Weekly 2 (MIBO2W)
       FI: Indices              O: Options   MIBO4W            XDMI FTSE MIB Options Weekly 4 (MIBO4W)
       FI: Indices              O: Options   MIBO5W            XDMI FTSE MIB Options Weekly 5 (MIBO5W)
       FI: Indices              O: Options                     XDMI FTSE MIB Options
       
       CI: Industrial Products               Y...              XDMI-IDEX Futures Annual
       CI: Industrial Products               Q...              XDMI-IDEX Futures Quaterly
       CI: Industrial Products               M...              XDMI-IDEX Futures Monthly
       CI: Industrial Products               D01.              XDMI-IDEX Futures Delivery
       CI: Industrial Products               D02.              XDMI-IDEX Futures Settlement
       
       CA: Agriculture...                    DWHMAD            XDMI-AGREX Futures Delivery
       CA: Agriculture...                    DWHMAW            XDMI-AGREX Futures Delivery
       CA: Agriculture...                                      XDMI-AGREX Futures 
       
       Si sur un DC considéré aucune des conditions ci-dessus n'est respectées on applique une Règle d'échéance
       par défaut intitulée : Default Rule -->
  <xsl:template name="getIDEM_MaturityRule">
    <xsl:param name="pContractSymbol"/>
    <xsl:param name="pUnderlyingAsset"/>
    <xsl:param name="pCategory"/>

    <xsl:choose>
      <!-- Stock-Equities -->
      <xsl:when test="$pUnderlyingAsset='FS'">
        <xsl:choose>
          <xsl:when test="$pCategory='F'">XDMI Equity Futures</xsl:when>
          <xsl:when test="$pCategory='O'">XDMI Equity Options</xsl:when>
          <!-- Unlikely -->
          <xsl:otherwise>
            <xsl:text>Default Rule</xsl:text>
            <!-- For debug only
            <xsl:text> - AssetCategory: </xsl:text>
            <xsl:value-of select="$pCategory" />
            -->
          </xsl:otherwise>
        </xsl:choose>
      </xsl:when>

      <!-- Indexes -->
      <xsl:when test="$pUnderlyingAsset='FI'">
        <xsl:choose>
          <xsl:when test="$pCategory='F'">
            <xsl:choose>
              <xsl:when test="$pContractSymbol='FDIV'">XDMI Dividend Futures</xsl:when>
              <xsl:when test="substring($pContractSymbol,1,1)='1'">XDMI Dividend Futures</xsl:when>
              <xsl:otherwise>XDMI FTSE MIB Futures</xsl:otherwise>
            </xsl:choose>
          </xsl:when>
          <xsl:when test="$pCategory='O'">
            <xsl:choose>
              <xsl:when test="$pContractSymbol='MIBO1W'">XDMI FTSE MIB Options Weekly 1 (MIBO1W)</xsl:when>
              <xsl:when test="$pContractSymbol='MIBO2W'">XDMI FTSE MIB Options Weekly 2 (MIBO2W)</xsl:when>
              <xsl:when test="$pContractSymbol='MIBO4W'">XDMI FTSE MIB Options Weekly 4 (MIBO4W)</xsl:when>
              <xsl:when test="$pContractSymbol='MIBO5W'">XDMI FTSE MIB Options Weekly 5 (MIBO5W)</xsl:when>
              <xsl:otherwise>XDMI FTSE MIB Options</xsl:otherwise>
            </xsl:choose>
          </xsl:when>
          <!-- Unlikely -->
          <xsl:otherwise>
            <xsl:text>Default Rule</xsl:text>
            <!-- For debug only
            <xsl:text> - AssetCategory: </xsl:text>
            <xsl:value-of select="$pCategory" />
            -->
          </xsl:otherwise>
        </xsl:choose>
      </xsl:when>

      <!-- CI	Industrial Products -->
      <xsl:when test="$pUnderlyingAsset='CI'">
        <xsl:choose>
          <xsl:when test="substring($pContractSymbol,1,1)='Y'">XDMI-IDEX Futures Annual</xsl:when>
          <xsl:when test="substring($pContractSymbol,1,1)='Q'">XDMI-IDEX Futures Quaterly</xsl:when>
          <xsl:when test="substring($pContractSymbol,1,1)='M'">XDMI-IDEX Futures Monthly</xsl:when>
          <xsl:when test="substring($pContractSymbol,1,1)='D'">XDMI-IDEX Futures Delivery</xsl:when>
          <xsl:otherwise>Default Rule</xsl:otherwise>
        </xsl:choose>
      </xsl:when>

      <!-- CA	Agriculture, forestry and fishing -->
      <xsl:when test="$pUnderlyingAsset='CA'">
        <xsl:choose>
          <xsl:when test="$pContractSymbol='DWHMAD'">XDMI-AGREX Futures Delivery</xsl:when>
          <xsl:when test="$pContractSymbol='DWHMAW'">XDMI-AGREX Futures Delivery</xsl:when>
          <xsl:otherwise>XDMI-AGREX Futures</xsl:otherwise>
        </xsl:choose>
      </xsl:when>

      <!-- Underlying Asset not managed -->
      <xsl:otherwise>
        <xsl:text>Default Rule</xsl:text>
        <!-- For debug only
        <xsl:text> - UnderlyingAsset: </xsl:text>
        <xsl:value-of select="$pUnderlyingAsset" />
        -->
      </xsl:otherwise>
    </xsl:choose>
  </xsl:template>

  <!-- *************************************************************************************************** -->
  <!-- getUnderlyingAsset - return UnderlyingAssetEnum from an IDEM Product Type                           -->
  <!-- *************************************************************************************************** -->
  <xsl:template name="getUnderlyingAsset">
    <!-- Valeurs de l'enum UnderlyingAssetEnum Spheres® (ISO 10962) :
			FB	Basket
			FS	Stock-Equities
			FD	Interest rate/notional debt securities
			FC	Currencies
			FI	Indices
			FO	Options
			FW	Swaps
			FT	Commodities
			CE	Extraction Resources
			CA	Agriculture, forestry and fishing
			CI	Industrial Products
			CS	Services
			M	Others
  	-->
    <xsl:param name="pExchangeSymbol"/>
    <xsl:param name="pProductType"/>

    <xsl:choose>
      <!-- IDEX -->
      <xsl:when test="$pExchangeSymbol = $gIDEX_ExchangeSymbol">
        <xsl:value-of select="'CI'"/>
      </xsl:when>
      <!-- AGREX -->
      <xsl:when test="$pExchangeSymbol = $gAGREX_ExchangeSymbol">
        <xsl:value-of select="'CA'"/>
      </xsl:when>
      <!-- IDEM -->
      <xsl:otherwise>
        <xsl:choose>
          <!-- IEquity -->
          <xsl:when test="$pProductType='E'">
            <xsl:value-of select="'FS'"/>
          </xsl:when>
          <!-- Index -->
          <xsl:when test="$pProductType='I'">
            <xsl:value-of select="'FI'"/>
          </xsl:when>
          <!-- Bond -->
          <xsl:when test="$pProductType='B'">
            <xsl:value-of select="'FD'"/>
          </xsl:when>
          <!-- Securities -->
          <xsl:when test="$pProductType='S'">
            <xsl:value-of select="'FD'"/>
          </xsl:when>
        </xsl:choose>
      </xsl:otherwise>
    </xsl:choose>
  </xsl:template>

  <!-- *************************************************************************************************** -->
  <!-- Cascading - return <table name="CONTRACTCASCADING" action="I"> from an IDEM Derivative Contract     -->
  <!-- *************************************************************************************************** -->
  <xsl:template name="Cascading">
    <xsl:param name="pSequenceno"/>
    <xsl:param name="pContractSymbol"/>
    <xsl:param name="pMATURITYMONTH" />
    <xsl:param name="pCASCContractSymbol"/>
    <xsl:param name="pCASCMATURITYMONTH"/>

    <table name="CONTRACTCASCADING" action="IU">
      <xsl:attribute name="sequenceno">
        <xsl:value-of select="$pSequenceno"/>
      </xsl:attribute>
      <column name="IDDC" datakey="true" datakeyupd="true" datatype="integer">
        <!-- BD 20130520 : Appel du template SQLDTENABLEDDTDISABLED pour vérifier la validité du DC sélectionné -->
        <SQL command="select" result="IDDC" cache="true">
          select dc.IDDC
          from dbo.DERIVATIVECONTRACT dc
          inner join dbo.MARKET m on (m.IDM=dc.IDM) and (m.ISO10383_ALPHA4='XDMI') and (m.EXCHANGESYMBOL=@EXCHANGESYMBOL)
          where (dc.CONTRACTSYMBOL=@CONTRACTSYMBOL)
          <xsl:call-template name="SQLDTENABLEDDTDISABLED">
            <xsl:with-param name="pTable" select="'dc'"/>
          </xsl:call-template>
          <Param name="DT" datatype="date">
            <xsl:value-of select="/iotask/parameters/parameter[@id='DTBUSINESS']"/>
          </Param>
          <Param name="EXCHANGESYMBOL" datatype="string">
            <xsl:value-of select="$gIDEX_ExchangeSymbol"/>
          </Param>
          <Param name="CONTRACTSYMBOL" datatype="string">
            <xsl:value-of select="$pContractSymbol"/>
          </Param>
        </SQL>
        <controls>
          <control action="RejectRow" return="true">
            <SpheresLib function="ISNULL()"/>
            <logInfo status="REJECT" isexception="true">
              <message>
                &lt;b&gt;Incorrect Derivative Contract.&lt;/b&gt;
                &lt;b&gt;Cause:&lt;/b&gt; The Derivative Contract symbol &lt;b&gt;<xsl:value-of select="$pContractSymbol"/>&lt;/b&gt; is not found.
                &lt;b&gt;Action:&lt;/b&gt; None.
                <xsl:text>&#xa;</xsl:text>xsl-t: <xsl:value-of select="$vFile"/><xsl:text>&#160;</xsl:text><xsl:value-of select="$vVersion"/><xsl:text>&#xa;</xsl:text>
              </message>
            </logInfo>
          </control>
        </controls>
      </column>
      <column name="MATURITYMONTH" datakey="true" datakeyupd="true" datatype="integer">
        <xsl:value-of select="$pMATURITYMONTH"/>
      </column>
      <column name="IDDC_CASC" datakey="true" datakeyupd="true" datatype="integer">
        <!-- BD 20130520 : Appel du template SQLDTENABLEDDTDISABLED pour vérifier la validité du DC sélectionné -->
        <SQL command="select" result="IDDC" cache="true">
          select dc.IDDC
          from dbo.DERIVATIVECONTRACT dc
          inner join dbo.MARKET m on (m.IDM=dc.IDM) and (m.ISO10383_ALPHA4='XDMI') and (m.EXCHANGESYMBOL=@EXCHANGESYMBOL)
          where (dc.CONTRACTSYMBOL=@CONTRACTSYMBOL)
          <xsl:call-template name="SQLDTENABLEDDTDISABLED">
            <xsl:with-param name="pTable" select="'dc'"/>
          </xsl:call-template>
          <Param name="DT" datatype="date">
            <xsl:value-of select="/iotask/parameters/parameter[@id='DTBUSINESS']"/>
          </Param>
          <Param name="EXCHANGESYMBOL" datatype="string">
            <xsl:value-of select="$gIDEX_ExchangeSymbol"/>
          </Param>
          <Param name="CONTRACTSYMBOL" datatype="string">
            <xsl:value-of select="$pCASCContractSymbol"/>
          </Param>
        </SQL>
        <controls>
          <control action="RejectRow" return="true">
            <SpheresLib function="ISNULL()"/>
            <logInfo status="REJECT" isexception="true">
              <message>
                &lt;b&gt;Incorrect Derivative Contract.&lt;/b&gt;
                &lt;b&gt;Cause:&lt;/b&gt; The Derivative Contract symbol &lt;b&gt;<xsl:value-of select="$pCASCContractSymbol"/>&lt;/b&gt; is not found.
                &lt;b&gt;Action:&lt;/b&gt; None.
                <xsl:text>&#xa;</xsl:text>xsl-t: <xsl:value-of select="$vFile"/><xsl:text>&#160;</xsl:text><xsl:value-of select="$vVersion"/><xsl:text>&#xa;</xsl:text>
              </message>
            </logInfo>
          </control>
        </controls>
      </column>
      <column name="CASCMATURITYMONTH" datakey="true" datakeyupd="true" datatype="integer">
        <xsl:value-of select="$pCASCMATURITYMONTH"/>
      </column>
      <column name="ISYEARINCREMENT" datakey="false" datakeyupd="true" datatype="bool">0</column>

      <xsl:call-template name="SysIns">
        <xsl:with-param name="pIsWithControl" select="$gFalse"/>
      </xsl:call-template>
    </table>
  </xsl:template>

</xsl:stylesheet>
