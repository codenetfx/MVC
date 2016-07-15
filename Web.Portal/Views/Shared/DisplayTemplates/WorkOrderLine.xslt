<?xml version="1.0" encoding="utf-8"?>
<xsl:stylesheet version="1.0"
								xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
								xmlns:xsltutility="urn:UL.Aria.Web.Common.Xslt.XslUtility"
								xmlns:workorderv1="http://xmlns.oracle.com/EnterpriseObjects/Core/EBO/WorkOrder/V1"
								xmlns:customv1="http://xmlns.oracle.com/EnterpriseObjects/Core/Custom/EBO/WorkOrder/V1"
								xmlns:commonv2="http://xmlns.oracle.com/EnterpriseObjects/Core/Common/V2"
								exclude-result-prefixes="xsl customv1 commonv2 workorderv1 xsltutility"
>
	<xsl:output method="html" indent="yes" omit-xml-declaration="yes" />
	<xsl:variable name="_cancelledStatus" select="'Cancelled'" />
  <xsl:variable name="orderItem" select="../OrderItem" />
  
    
	<xsl:template match="/OrderServicesSearchResult">
		<xsl:choose>
			<xsl:when test="workorderv1:WorkOrderLine">
				<xsl:apply-templates select="workorderv1:WorkOrderLine" />
			</xsl:when>
			<xsl:otherwise>
				<xsl:apply-templates select="OrderItem" />
			</xsl:otherwise>
		</xsl:choose>
	</xsl:template>

	<xsl:template match="OrderItem">
		<div class="search-result service-result clearfix">
			<div class="result-icon">
				<img src="/images/clear.gif" alt="Services" />
			</div>

			<div class="result-detail">
				<div class="result-item-header">
					<div class="display-status">
						<xsl:value-of select="Status" />
							<span class="display-status-pipe">|</span>  
							<span class="display-status-capitalize">Customer Product Service Identifier: </span> 

							<xsl:value-of select="CustomerModelNumber" />

					</div>
				</div>

				<div class="result-item-title">
						<xsl:value-of select="concat(LineNumber, ' ', ItemName, ' (', Quantity, ')')" />
				</div>

				<!--<div class="display-row status-row">
					<label>
						<xsl:text>0 Samples</xsl:text>
					</label>
				</div>
				<div class="display-row status-row">
				<label>
						<xsl:text>0 Tests</xsl:text>
					</label>
				</div>-->

			</div>
		</div>
	</xsl:template>
	
	<xsl:template match="workorderv1:WorkOrderLine">
		<xsl:variable name="status">
			<xsl:call-template name="Status">
				<xsl:with-param name="status" select="commonv2:Status" />
			</xsl:call-template>
		</xsl:variable>

		<div class="search-result service-result clearfix">
			<div class="result-icon">
				<img src="/images/clear.gif" alt="Services" />
			</div>

			<div class="result-detail">
				<div class="result-item-header" style="width: 100%">
					<div class="display-status">
						<xsl:value-of select="$status" />

						<span class="display-status-pipe">|</span>  
						<span class="display-status-capitalize">Customer Product Service Identifier: </span> 

            <xsl:value-of select="CustomerModelNumber" />
          	

        </div>
				</div>

				<div class="result-item-title">
					<xsl:value-of select="concat(commonv2:Identification/commonv2:ApplicationObjectKey/commonv2:ContextID | $orderItem/LineNumber, ' ', commonv2:Description | $orderItem/ItemName, ' (', $orderItem/Quantity, ')')" />
				</div>

				<div class="display-row status-row">
					<label>
						<xsl:value-of select="concat(count(workorderv1:Custom/customv1:Sample), ' Samples')" />
					</label>
					<xsl:variable name="sampleStatusList" select="workorderv1:Custom/customv1:Sample/commonv2:Status" />
					<xsl:call-template name="GenerateStatusCounts">
						<xsl:with-param name="statusList" select="$sampleStatusList" />
						<xsl:with-param name="currentIndex" select="1" />
						<xsl:with-param name="count" select="count($sampleStatusList)" />
						<xsl:with-param name="keys" select="''" />
					</xsl:call-template>
				</div>

				<div class="display-row status-row">
					<label>
						<xsl:value-of select="concat(count(workorderv1:Custom/customv1:Sample/customv1:Test), ' Tests')" />
					</label>
					<xsl:variable name="testStatusList" select="workorderv1:Custom/customv1:Sample/customv1:Test/commonv2:Status" />
					<xsl:call-template name="GenerateStatusCounts">
						<xsl:with-param name="statusList" select="$testStatusList" />
						<xsl:with-param name="currentIndex" select="1" />
						<xsl:with-param name="count" select="count($testStatusList)" />
						<xsl:with-param name="keys" select="''" />
					</xsl:call-template>
				</div>
			</div>

			<div class="view-samples clear-both">
				<xsl:if test="workorderv1:Custom/customv1:Sample">
					<a href="#" class="order-sample-up-arrow">
						<i></i>
						<xsl:text>View Samples &amp; Test Results</xsl:text>
				</a>
					<div class="div-sample-lines" style="display:none;">
						<xsl:apply-templates mode="Sample" select="workorderv1:Custom/customv1:Sample" />
					</div>
				</xsl:if>
			</div>

		</div>
	</xsl:template>

	<xsl:template name="GenerateStatusCounts">
		<xsl:param name="statusList" />
		<xsl:param name="currentIndex" />
		<xsl:param name="count" />
		<xsl:param name="keys" />
		<xsl:variable name="status" select="$statusList[$currentIndex]/commonv2:Description" />
		<xsl:variable name="statusKey" select="concat('[', $status, ']')" />
		<xsl:variable name="statusIcon" select="concat('ul-icon-', xsltutility:ToLower(translate($status, ' ', '')))" />

		<xsl:if test="$currentIndex &lt;= $count">
			<xsl:choose>
				<xsl:when test="contains($keys, $statusKey)">
					<xsl:call-template name="GenerateStatusCounts">
						<xsl:with-param name="statusList" select="$statusList" />
						<xsl:with-param name="currentIndex" select="$currentIndex + 1" />
						<xsl:with-param name="count" select="$count" />
						<xsl:with-param name="keys" select="$keys" />
					</xsl:call-template>
				</xsl:when>
				<xsl:otherwise>
					<div class="display-label-short">
						<i class="{$statusIcon}" />
						<xsl:value-of select="concat(count($statusList/commonv2:Description[. = $status]), ' ', $status)" />
					</div>
					<xsl:call-template name="GenerateStatusCounts">
						<xsl:with-param name="statusList" select="$statusList" />
						<xsl:with-param name="currentIndex" select="$currentIndex + 1" />
						<xsl:with-param name="count" select="$count" />
						<xsl:with-param name="keys" select="concat($keys, '[', $status, ']')" />
					</xsl:call-template>
				</xsl:otherwise>
			</xsl:choose>
		</xsl:if>
	</xsl:template>

	<xsl:template name="BusinessComponentID">
		<xsl:param name="identification" />
		<xsl:value-of select="$identification/commonv2:BusinessComponentID" />
	</xsl:template>

	<xsl:template name="Status">
		<xsl:param name="status" />
		<xsl:value-of select="$status/commonv2:Description" />
	</xsl:template>

	
	<xsl:template mode="Sample" match="workorderv1:Custom/customv1:Sample">
		<xsl:variable name="businessComponentId">
			<xsl:call-template name="BusinessComponentID">
				<xsl:with-param name="identification" select="commonv2:Identification" />
			</xsl:call-template>
		</xsl:variable>
		<xsl:variable name="status">
			<xsl:call-template name="Status">
				<xsl:with-param name="status" select="commonv2:Status" />
			</xsl:call-template>
		</xsl:variable>
		<xsl:variable name="totalResults" select="count(customv1:Test[commonv2:Status/commonv2:Description != $_cancelledStatus]/customv1:Result)" />
		<xsl:variable name="totalResultsFinished" select="count(customv1:Test[commonv2:Status/commonv2:Description != $_cancelledStatus]/customv1:Result/customv1:ResultValue[string-length(.) > 0])" />
		<xsl:variable name="percentageFinished" select="round($totalResultsFinished div $totalResults * 100)" />
		<xsl:variable name="barStatus">
			<xsl:choose>
				<xsl:when test="(customv1:Test/customv1:Result/customv1:Inspec | customv1:Test/customv1:Result/customv1:InSpec) = 'F'">bar-danger</xsl:when>
				<xsl:otherwise>bar-success</xsl:otherwise>
			</xsl:choose>
		</xsl:variable>

		<div class="clear-both order-sample-tests">
			<div class="order-samples">
				<div class="order-samples-left">
					<a class="toggle-open" href="#tests">
						<xsl:if test="not(customv1:Test)">
							<xsl:attribute name="class">
								<xsl:text>toggle-nothing</xsl:text>
							</xsl:attribute>
						</xsl:if>
						<xsl:value-of select="concat($businessComponentId, ' - ', customv1:Description, ': ', $status)" />
					</a>
				</div>
				<div class="progress">
					<div class="bar {$barStatus}" style="width:{$percentageFinished}%"></div>
				</div>
			</div>
			<!--
			This element is where you would display:none if you wanted to hide TESTS by default
			NOTE: keep the toggle-open/toggle-close class above in sync here
			-->
			<div class="order-samples-result" style="display:none;">
				<xsl:apply-templates mode="Test" select="customv1:Test" />
			</div>
		</div>
	</xsl:template>

	<!--
	Cancelled Tests
	-->
	<xsl:template mode="Test" match="customv1:Test[commonv2:Status/commonv2:Description = 'Cancelled']">
		<xsl:variable name="businessComponentId">
			<xsl:call-template name="BusinessComponentID">
				<xsl:with-param name="identification" select="commonv2:Identification" />
			</xsl:call-template>
		</xsl:variable>

		<div class="clear-both">
			<xsl:if test="position() = 1">
				<xsl:attribute name="class">
					<xsl:text>clear-both first</xsl:text>
				</xsl:attribute>
			</xsl:if>
			<div class="order-tests">
				<div class="order-tests-left">
					<a class="toggle-nothing" href="#test-results">
						<xsl:value-of select="concat(customv1:Analysis, ' (', $businessComponentId, '): Cancelled')" />
					</a>
				</div>
				<div class="progress">
					<div class="bar bar-success" style="width:0%"></div>
				</div>
			</div>
		</div>
	</xsl:template>

	<!--
	All other Tests
	-->
	<xsl:template mode="Test" match="customv1:Test">
		<xsl:variable name="businessComponentId">
			<xsl:call-template name="BusinessComponentID">
				<xsl:with-param name="identification" select="commonv2:Identification" />
			</xsl:call-template>
		</xsl:variable>
		<xsl:variable name="totalResults" select="count(customv1:Result)" />
		<xsl:variable name="totalResultsFinished" select="count(customv1:Result/customv1:ResultValue[string-length(.) > 0])" />
		<xsl:variable name="percentageFinished" select="round($totalResultsFinished div $totalResults * 100)" />
		<xsl:variable name="barStatus">
			<xsl:choose>
				<xsl:when test="(customv1:Result/customv1:Inspec | customv1:Result/customv1:InSpec) = 'F'">bar-error</xsl:when>
				<xsl:otherwise>bar-success</xsl:otherwise>
			</xsl:choose>
		</xsl:variable>

		<div class="clear-both">
			<xsl:if test="position() = 1">
				<xsl:attribute name="class">
					<xsl:text>clear-both first</xsl:text>
				</xsl:attribute>
			</xsl:if>
			<div class="order-tests">
				<div class="order-tests-left">
					<a class ="toggle-open" href="#test-results">
						<xsl:if test="not(customv1:Result)">
							<xsl:attribute name="class">
								<xsl:text>toggle-nothing</xsl:text>
							</xsl:attribute>
						</xsl:if>
						<xsl:value-of select="concat(customv1:Analysis, ' (', $businessComponentId, ')')" />
					</a>
				</div>
				<div class="progress">
					<div class="bar {$barStatus}" style="width:{$percentageFinished}%"></div>
				</div>
			</div>

			<!--
			This element is where you would display:none if you wanted to hide TEST RESULTS by default
			NOTE: keep the toggle-open/toggle-close class above in sync here
			-->
			<div class="order-tests-result" style="display:none;">
				<table>
					<thead>
						<tr>
							<th class="nowrap result-status"></th>
							<th class="nowrap result-component">Component</th>
							<th class="nowrap">Specifications</th>
							<th class="nowrap">Results</th>
						</tr>
					</thead>
					<xsl:apply-templates mode="Result" select="customv1:Result" />
				</table>
			</div>
		</div>
	</xsl:template>

	<xsl:template mode="Result" match="customv1:Result">
		<xsl:variable name="units" select="customv1:Units" />
		<xsl:variable name="hasMin" select="boolean(customv1:Min) and string-length(customv1:Min) > 0" />
		<xsl:variable name="hasMax" select="boolean(customv1:Max) and string-length(customv1:Max) > 0" />
		<xsl:variable name="resultStatus">
			<xsl:choose>
				<xsl:when test="customv1:ResultValue[string-length(.) > 0]">
					<xsl:choose>
						<xsl:when test="(customv1:Inspec | customv1:InSpec) = 'F'">result-error</xsl:when>
						<xsl:otherwise>result-success</xsl:otherwise>
					</xsl:choose>
				</xsl:when>
				<xsl:otherwise>result-pending</xsl:otherwise>
			</xsl:choose>
		</xsl:variable>
		<tr class="result {$resultStatus}">
			<td>
				<i class="result-status" />
			</td>
			<td>
				<xsl:value-of select="customv1:Component" />
			</td>
			<td>
				<xsl:choose>
					<xsl:when test="$hasMin and $hasMax">
						<xsl:value-of select="concat(customv1:Min, '-', customv1:Max, $units)" />
					</xsl:when>
					<xsl:when test="$hasMin">
						<xsl:value-of select="customv1:Min" />
						<xsl:value-of select="$units" />
					</xsl:when>
					<xsl:when test="$hasMax">
						<xsl:value-of select="customv1:Max" />
						<xsl:value-of select="$units" />
					</xsl:when>
					<xsl:otherwise>&#160;</xsl:otherwise>
				</xsl:choose>
			</td>
			<td>
				<xsl:choose>
					<xsl:when test="customv1:ResultValue = ''">Pending</xsl:when>
					<xsl:otherwise>
						<xsl:value-of select="customv1:ResultValue" />
						<xsl:value-of select="$units" />
					</xsl:otherwise>
				</xsl:choose>
			</td>
		</tr>
	</xsl:template>
</xsl:stylesheet>
