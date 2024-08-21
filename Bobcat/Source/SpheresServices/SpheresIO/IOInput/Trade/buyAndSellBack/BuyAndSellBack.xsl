<?xml version="1.0" encoding="UTF-8"?>
<xsl:stylesheet xmlns:xsl="http://www.w3.org/1999/XSL/Transform" version="1.0">
	<xsl:output method="xml" omit-xml-declaration="no" encoding="UTF-8" indent="yes" media-type="text/xml;"/>

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
			<xsl:apply-templates select="parameters"/>
			<xsl:apply-templates select="iotaskdet"/>
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
			<xsl:apply-templates select="ioinput"/>
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
			<xsl:apply-templates select="file"/>
		</ioinput>
	</xsl:template>

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
			<xsl:apply-templates select="row[@status='success']"/>
		</file>
	</xsl:template>

	<xsl:template match="row">
		<row>
			<xsl:attribute name="id">
				<xsl:value-of select="@id"/>
			</xsl:attribute>
			<xsl:attribute name="src">
				<xsl:value-of select="@src"/>
			</xsl:attribute>
			<tradeImport>
				<settings>
					<importMode>New</importMode>
					<parameters>
						<parameter name="http://www.efs.org/otcml/tradeImport/instrumentIdentifier" datatype="string">buyAndSellBack</parameter>
						<parameter name="http://www.efs.org/otcml/tradeImport/templateIdentifier" datatype="string">buyAndSellBack</parameter>
						<!-- screen from instrument default -->
						<parameter name="http://www.efs.org/otcml/tradeImport/screen" datatype="string">buyAndSellBack</parameter>
						<!--/////-->
						<parameter name="http://www.efs.org/otcml/tradeImport/displayname" datatype="string">buyAndSellBack Import Sample</parameter>
						<parameter name="http://www.efs.org/otcml/tradeImport/description" datatype="string">buyAndSellBack Import Sample</parameter>
						<parameter name="http://www.efs.org/otcml/tradeImport/extllink" datatype="string" />
						<!--/////-->
						<parameter name="http://www.efs.org/otcml/tradeImport/isApplyFeeCalculation" datatype="bool">false</parameter>
						<parameter name="http://www.efs.org/otcml/tradeImport/isApplyPartyTemplate" datatype="bool">false</parameter>
						<parameter name="http://www.efs.org/otcml/tradeImport/isPostToEventsGen" datatype="bool">true</parameter>
					</parameters>
				</settings>
				<tradeInput>
					<customCaptureInfos>
						<!-- ******* -->
						<!-- Parties -->
						<!-- ******* -->
						
						<!-- Party1 -->
						<customCaptureInfo clientId="tradeHeader_party1_actor" dataType="string">
							<value>
								<xsl:value-of select="data[@name='ACTOR1']"/>
							</value >
						</customCaptureInfo>
						<customCaptureInfo clientId="tradeHeader_party1_book" dataType="string">
							<value>
								<xsl:value-of select="data[@name='ACTOR1_BOOK']"/>
							</value >
						</customCaptureInfo>
						<customCaptureInfo clientId="tradeHeader_party1_localClassNDrv" dataType="string">
							<value>
								<xsl:value-of select="data[@name='ACTOR1_LOCALCLASS']"/>
							</value>
						</customCaptureInfo>
						<customCaptureInfo clientId="tradeHeader_party1_iasClassNDrv" dataType="string">
							<value>
								<xsl:value-of select="data[@name='ACTOR1_IASCLASS']"/>
							</value>
						</customCaptureInfo>
						<customCaptureInfo clientId="tradeHeader_party1_hedgeClassNDrv" dataType="string">
							<value />
						</customCaptureInfo>
						<customCaptureInfo clientId="tradeHeader_party1_trader1_identifier" dataType="string">
							<value/>
						</customCaptureInfo>
						<customCaptureInfo clientId="tradeHeader_party1_frontId" dataType="string">
							<value/>
						</customCaptureInfo>
						<customCaptureInfo clientId="tradeHeader_party1_folder" dataType="string">
							<value />
						</customCaptureInfo>						
						<customCaptureInfo clientId="tradeHeader_party1_sales1_identifier" dataType="string">
							<value />
						</customCaptureInfo>
						<customCaptureInfo clientId="tradeHeader_party1_sales1_factor" dataType="decimal">
							<value />
						</customCaptureInfo>
						<customCaptureInfo clientId="tradeHeader_party1_broker1_actor" dataType="string">
							<value />
						</customCaptureInfo>
						<customCaptureInfo clientId="tradeHeader_party1_broker1_frontId" dataType="string">
							<value />
						</customCaptureInfo>
						<customCaptureInfo clientId="tradeHeader_party1_broker1_trader1_identifier" dataType="string">
							<value />
						</customCaptureInfo>

						<!-- Party2 -->
						<customCaptureInfo clientId="tradeHeader_party2_actor" dataType="string">
							<value>
								<xsl:value-of select="data[@name='ACTOR2']"/>
							</value >
						</customCaptureInfo>
						<customCaptureInfo clientId="tradeHeader_party2_book" dataType="string">
							<value>
								<xsl:value-of select="data[@name='ACTOR2_BOOK']"/>
							</value >
						</customCaptureInfo>
						<customCaptureInfo clientId="tradeHeader_party2_localClassNDrv" dataType="string">
							<value>
								<xsl:value-of select="data[@name='ACTOR2_LOCALCLASS']"/>
							</value>
						</customCaptureInfo>
						<customCaptureInfo clientId="tradeHeader_party2_iasClassNDrv" dataType="string">
							<value>
								<xsl:value-of select="data[@name='ACTOR2_IASCLASS']"/>
							</value>
						</customCaptureInfo>
						<customCaptureInfo clientId="tradeHeader_party2_hedgeClassNDrv" dataType="string">
							<value />
						</customCaptureInfo>
						<customCaptureInfo clientId="tradeHeader_party2_trader1_identifier" dataType="string">
							<value/>
						</customCaptureInfo>
						<customCaptureInfo clientId="tradeHeader_party2_frontId" dataType="string">
							<value/>
						</customCaptureInfo>
						<customCaptureInfo clientId="tradeHeader_party2_folder" dataType="string">
							<value />
						</customCaptureInfo>
						<customCaptureInfo clientId="tradeHeader_party2_sales1_identifier" dataType="string">
							<value />
						</customCaptureInfo>
						<customCaptureInfo clientId="tradeHeader_party2_sales1_factor" dataType="decimal">
							<value />
						</customCaptureInfo>
						<customCaptureInfo clientId="tradeHeader_party2_broker1_actor" dataType="string">
							<value />
						</customCaptureInfo>
						<customCaptureInfo clientId="tradeHeader_party2_broker1_frontId" dataType="string">
							<value />
						</customCaptureInfo>
						<customCaptureInfo clientId="tradeHeader_party2_broker1_trader1_identifier" dataType="string">
							<value />
						</customCaptureInfo>

						<!-- *********** -->
						<!-- tradeHeader -->
						<!-- *********** -->
						<customCaptureInfo clientId="tradeHeader_tradeDate" dataType="date">
							<value>
								<xsl:value-of select="data[@name='TRADE_DATE']"/>
							</value>
						</customCaptureInfo>
						<customCaptureInfo clientId="tradeHeader_timeStamping" dataType="time" regex="RegexLongTime">
							<value>
								<xsl:value-of select="data[@name='TRADE_HHMMSS']"/>
							</value>
						</customCaptureInfo>

						<!-- *********** -->
						<!-- cashStream  -->
						<!-- *********** -->
						<customCaptureInfo clientId="bsb_cashStream1_payer" dataType="string">
							<xsl:element name ="value">
								<xsl:value-of select="data[@name='bsb_cashStream_payer']"/>
							</xsl:element>
						</customCaptureInfo>
						<customCaptureInfo clientId="bsb_cashStream1_calculationPeriodAmount_calculation_fixedRateSchedule_initialValue" dataType="string" regex="RegexRate">
							<xsl:element name ="value">
								<xsl:value-of select="data[@name='bsb_cashStream_fixedRateSchedule_initialValue']"/>
							</xsl:element>
						</customCaptureInfo>
						<customCaptureInfo clientId="bsb_cashStream1_calculationPeriodAmount_calculation_dayCountFraction" dataType="string">
							<xsl:element name ="value">
								<xsl:value-of select="data[@name='bsb_cashStream_dayCountFraction']"/>
							</xsl:element>
						</customCaptureInfo>
						
						<!-- ***************** -->
						<!-- BuyAndSellBack    -->
						<!-- ***************** -->
						<customCaptureInfo clientId="bsb_effectiveMinDate" dataType="date">
							<value>
								<xsl:value-of select="data[@name='bsb_effectiveMinDate']"/>
							</value>
						</customCaptureInfo>
						<customCaptureInfo clientId="bsb_terminationMaxDate" dataType="date">
							<value>
								<xsl:value-of select="data[@name='bsb_terminationMaxDate']"/>
							</value>
						</customCaptureInfo>
						
						<!-- ************* -->
						<!-- BSB_SpotLeg1 -->
						<!-- ************* -->
						<customCaptureInfo clientId="bsb_spotLeg1_debtSecurityTransaction_securityAsset_securityId" dataType="string">
							<value>
								<xsl:value-of select="data[@name='securityAsset_securityId']"/>
							</value>
						</customCaptureInfo>

						<!-- ************* -->
						<!-- orderQuantity -->
						<!-- ************* -->
						<customCaptureInfo clientId="bsb_spotLeg1_debtSecurityTransaction_orderQuantity_quantityType" dataType="string">
							<xsl:element name ="value">
								<xsl:value-of select="data[@name='orderQuantity_quantityType']"/>
							</xsl:element>
						</customCaptureInfo>
						<customCaptureInfo clientId="bsb_spotLeg1_debtSecurityTransaction_orderQuantity_quantityAmount" dataType="decimal">
							<xsl:element name ="value">
								<xsl:value-of select="data[@name='orderQuantity_quantityAmount']"/>
							</xsl:element>
						</customCaptureInfo>

						<!-- ********** -->
						<!-- orderPrice -->
						<!-- ********** -->
						<customCaptureInfo clientId="bsb_spotLeg1_debtSecurityTransaction_orderPrice_assetMesure" dataType="string">
							<xsl:element name ="value">
								<xsl:value-of select="data[@name='orderPrice_assetMesure']"/>
							</xsl:element>
						</customCaptureInfo>
						<customCaptureInfo clientId="bsb_spotLeg1_debtSecurityTransaction_orderPrice_priceUnits" dataType="string">
							<xsl:element name ="value">
								<xsl:value-of select="data[@name='orderPrice_priceUnits']"/>
							</xsl:element>
						</customCaptureInfo>
						<customCaptureInfo clientId="bsb_spotLeg1_debtSecurityTransaction_orderPrice_price" dataType="decimal">
							<xsl:element name ="value">
								<xsl:value-of select="data[@name='orderPrice_price']"/>
							</xsl:element>
						</customCaptureInfo>
						<customCaptureInfo clientId="bsb_spotLeg1_debtSecurityTransaction_orderPrice_accruedInterestRate" dataType="decimal">
							<xsl:element name ="value">
								<xsl:value-of select="data[@name='orderPrice_accruedInterestRate']"/>
							</xsl:element>
						</customCaptureInfo>
						<customCaptureInfo clientId="bsb_spotLeg1_debtSecurityTransaction_orderPrice_accruedInterestAmount_amount" dataType="decimal">
							<xsl:element name ="value">
								<xsl:value-of select="data[@name='orderPrice_accruedInterestAmount_amount']"/>
							</xsl:element>
						</customCaptureInfo>

						<!-- *********** -->
						<!-- grossAmount -->
						<!-- *********** -->
						<customCaptureInfo clientId="bsb_spotLeg1_debtSecurityTransaction_grossAmount_payer" dataType="string">
							<xsl:element name ="value">
								<xsl:value-of select="data[@name='grossAmount_payer']"/>
							</xsl:element>
						</customCaptureInfo>
						<customCaptureInfo clientId="bsb_spotLeg1_debtSecurityTransaction_grossAmount_paymentDate_unadjustedDate" dataType="date">
							<xsl:element name ="value">
								<xsl:value-of select="data[@name='grossAmount_paymentDate_unadjustedDate']"/>
							</xsl:element>
						</customCaptureInfo>
						<customCaptureInfo clientId="bsb_spotLeg1_debtSecurityTransaction_grossAmount_paymentAmount_amount" dataType="decimal">
							<xsl:element name ="value">
								<xsl:value-of select="data[@name='grossAmount_paymentAmount_amount']"/>
							</xsl:element>
						</customCaptureInfo>
						<customCaptureInfo clientId="bsb_spotLeg1_debtSecurityTransaction_grossAmount_paymentAmount_currency" dataType="string">
							<xsl:element name ="value">
								<xsl:value-of select="data[@name='grossAmount_paymentAmount_currency']"/>
							</xsl:element>
						</customCaptureInfo>

						<!-- *************** -->
						<!-- BSB_ForwardLeg1 -->
						<!-- *************** -->
						
						<!-- ************* -->
						<!-- orderQuantity -->
						<!-- ************* -->
						<customCaptureInfo clientId="bsb_forwardLeg1_debtSecurityTransaction_orderQuantity_quantityType" dataType="string">
							<xsl:element name ="value">
								<xsl:value-of select="data[@name='forwardLeg_orderQuantity_quantityType']"/>
							</xsl:element>
						</customCaptureInfo>
						<customCaptureInfo clientId="bsb_forwardLeg1_debtSecurityTransaction_orderQuantity_quantityAmount" dataType="decimal">
							<xsl:element name ="value">
								<xsl:value-of select="data[@name='forwardLeg_orderQuantity_quantityAmount']"/>
							</xsl:element>
						</customCaptureInfo>

						<!-- ********** -->
						<!-- orderPrice -->
						<!-- ********** -->
						<customCaptureInfo clientId="bsb_forwardLeg1_debtSecurityTransaction_orderPrice_assetMesure" dataType="string">
							<xsl:element name ="value">
								<xsl:value-of select="data[@name='forwardLeg_orderPrice_assetMesure']"/>
							</xsl:element>
						</customCaptureInfo>
						<customCaptureInfo clientId="bsb_forwardLeg1_debtSecurityTransaction_orderPrice_priceUnits" dataType="string">
							<xsl:element name ="value">
								<xsl:value-of select="data[@name='forwardLeg_orderPrice_priceUnits']"/>
							</xsl:element>
						</customCaptureInfo>
						<customCaptureInfo clientId="bsb_forwardLeg1_debtSecurityTransaction_orderPrice_price" dataType="decimal">
							<xsl:element name ="value">
								<xsl:value-of select="data[@name='forwardLeg_orderPrice_price']"/>
							</xsl:element>
						</customCaptureInfo>
						<customCaptureInfo clientId="bsb_forwardLeg1_debtSecurityTransaction_orderPrice_accruedInterestRate" dataType="decimal">
							<xsl:element name ="value">
								<xsl:value-of select="data[@name='forwardLeg_orderPrice_accruedInterestRate']"/>
							</xsl:element>
						</customCaptureInfo>
						<customCaptureInfo clientId="bsb_forwardLeg1_debtSecurityTransaction_orderPrice_accruedInterestAmount_amount" dataType="decimal">
							<xsl:element name ="value">
								<xsl:value-of select="data[@name='forwardLeg_orderPrice_accruedInterestAmount_amount']"/>
							</xsl:element>
						</customCaptureInfo>

						<!-- *********** -->
						<!-- grossAmount -->
						<!-- *********** -->
						<customCaptureInfo clientId="bsb_forwardLeg1_debtSecurityTransaction_grossAmount_payer" dataType="string">
							<xsl:element name ="value">
								<xsl:value-of select="data[@name='forwardLeg_grossAmount_payer']"/>
							</xsl:element>
						</customCaptureInfo>
						<customCaptureInfo clientId="bsb_forwardLeg1_debtSecurityTransaction_grossAmount_paymentDate_unadjustedDate" dataType="date">
							<xsl:element name ="value">
								<xsl:value-of select="data[@name='forwardLeg_grossAmount_paymentDate_unadjustedDate']"/>
							</xsl:element>
						</customCaptureInfo>
						<customCaptureInfo clientId="bsb_forwardLeg1_debtSecurityTransaction_grossAmount_paymentAmount_amount" dataType="decimal">
							<xsl:element name ="value">
								<xsl:value-of select="data[@name='forwardLeg_grossAmount_paymentAmount_amount']"/>
							</xsl:element>
						</customCaptureInfo>
						<customCaptureInfo clientId="bsb_forwardLeg1_debtSecurityTransaction_grossAmount_paymentAmount_currency" dataType="string">
							<xsl:element name ="value">
								<xsl:value-of select="data[@name='forwardLeg_grossAmount_paymentAmount_currency']"/>
							</xsl:element>
						</customCaptureInfo>
					</customCaptureInfos>
				</tradeInput>
			</tradeImport>
		</row>
	</xsl:template >

</xsl:stylesheet>
