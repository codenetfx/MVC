/// <reference path="../_references.js" />
/// <reference path="../_ULReferences.js" />
/// <reference path="UL.AutoComplete.js" />
/// <reference path="../../Lib/jquery/plugins/jquery.storageapi.js" />
/// <reference path="../../Lib/jquery/plugins/jquery.jqGrid.src.js" />
/// <reference path="UL.MiscSupport.js" />


(function ($) {
	'use strict';
	if (!window.UL) {
		window.UL = {};
	}

	//#region MvcJqGrid Namespace and Static Class
	var MvcJqGrid = {
		///<field name="grids">Contains a Dictionary of all initialized Grids</field>
		///<field name="StateItemKeys">Provides an objec to contain const keys for mapping propertys to jqgird.</field>
		DataKey: "UL.MvcJqGrid",
		grids: {},
		Formatters: {},
		Renderers: {},
		Editors: {},
		ColumnTemplates: {},
		EditorEventHandlers: {},
		RegisteredEditorRules: {},
		EventNames: {
			SelectedRowChanged: "selectRowChanged.MvcJqGrid"
		},
		EditorRuleModes: {
			BeforeSave: "BeforeSave",
			Edit: "Edit",
			BeforeEdit: "BeforeEdit",
			MultiRowSelected: "MultiRowSelected",
			BeforeDelete: "BeforeDelete"
		},
		MultiMenuActions: {
			None: "multiNone",
			Delete: "multiDelete",
			Update: "multiUpdate"
		},
		StateItemKeys: {
			URL: 'url',
			SORT_NAME: 'sortname',
			SORT_ORDER: 'sortorder',
			SELECTED_ROW: 'selrow',
			PAGE: 'page',
			ROW_NUM: 'rowNum',
			POST_DATA: 'postData',
			SEARCH: 'search',
			COLUMNS: 'colModel'
		},
		EditorTypes:
        {
        	Default: "Default",
        	DatePicker: "DatePicker",
        	AutoComplete: "AutoComplete",
        	DropDown: "DropDown",
        	Checkbox: "Checkbox",
        	InputMask: "InputMask",
        	Custom: "Custom"
        },
		ColumnTemplateTypes: {
			ActionMenu: "ActionMenu",
			Icons: "Icons",
			ValidtionSummary: "vsummary"
		},
		DataFormatters: {
			None: 'None',
			Number: 'Number',
			Date: 'Date',
			Checkbox: "Checkbox",
			Custom: 'Custom'
		},

		RenderBehavior: {
			Default: 'Default',
			Link: 'Link',
			EditActions: "EditActions",
			Icons: "Icons",
			ValidationSummary: "ValidtionSummary",
			SimpleTooltip: "SimpleTooltip",
			ListTooltip: "ListTooltip",
			ValidationTooltip: "vtooltip",
			DropDown: "DropDown",
			CellMarker: "CellMarker",
			ActionMenu: "ActionMenu"
		},
		AttributeNames: {
			isDirty: "data-is-dirty"
		},
		WarnForChangeLossEnabled: false,
		IsInEditMode: function () {
			return $("tr[editable=1]").length > 0;
		},
		BeforeUnloadHandler: function (e) {
			/// <summary>
			/// Window before unload hander used to stop
			/// user from leave the page and loosing unsaved changes.
			/// </summary>
			/// <param name="e"></param>

			if (UL.MvcJqGrid.IsInEditMode()) {
				var evt = e || window.event;
				var warningMsg = "You currently have unsaved changes in the task grid.  If you leave the page, all changes will be lost.";

				if (evt) {
					evt.returnValue = warningMsg;
				}
				return warningMsg;
			}
		},
		EnableGlobalLossPrevention: function () {
			/// <summary>
			/// Registers an event to warn and allow for cancelling the user from 
			/// leaving the page when any grid is in edit mode.
			/// </summary>

			if (!MvcJqGrid.WarnForChangeLossEnabled) {
				var handler = MvcJqGrid.BeforeUnloadHandler;
				if (window.addEventListener) {
					window.addEventListener("beforeunload", handler, true);
				} else if (window.attachEvent) {
					window.attachEvent("onbeforeunload", handler);
				} else if (!(window.onbeforeunload === undefined)) {
					window.onbeforeunload = handler;
				} else {
					window.alert("Your browser does not support page navigation alerts. If the task grid has unsaved changes and you navigate away from the page, you will not be notified.");
				}
			}

			MvcJqGrid.WarnForChangeLossEnabled = true;
		},
		DisableGlobalLossPrevention: function () {
			/// <summary>
			/// Unregisters an event to warn and allow for cancelling the user from 
			/// leaving the page when any grid is in edit mode.
			/// </summary>

			if (MvcJqGrid.WarnForChangeLossEnabled) {
				var handler = MvcJqGrid.BeforeUnloadHandler;
				if (window.removeEventListener) {
					window.removeEventListener("beforeunload", handler);
				} else if (window.detachEvent) {
					window.detachEvent("onbeforeunload", handler);
				} else if (!(window.onbeforeunload === undefined)) {
					window.onbeforeunload = null;
				}
			}

		},
		FormatRouter: function (cellValue, options, rowObject) {
			/// <summary>
			/// Function that hooks into the jqgrid api to provided decesion 
			/// behavior for routing format requests to the appropriate format stategy function.
			/// </summary>
			/// <param name="cellValue">the value of the cell</param>
			/// <param name="options">The column model</param>
			/// <param name="rowObject">The row data object.</param>
			/// <returns type="String">The formatted value.</returns>

			var column = options.colModel;
			//cellValue = cellValue || rowObject[options.colModel.name];
			var formattedData = cellValue;

			if (column.formatInfo
                && column.formatInfo.type
                && MvcJqGrid.Formatters[column.formatInfo.type]
                && formattedData !== "") {

				formattedData = MvcJqGrid.Formatters[column.formatInfo.type]
                    .format(column.formatInfo.formatString, formattedData);
			}

			if (column.render && MvcJqGrid.Renderers[column.render]) {

				formattedData = MvcJqGrid.Renderers[column.render]
                    .behavior(formattedData, column, rowObject);
			}

			return formattedData || "";
		},
		UnFormatRouter: function (cellText, options, cellValue) {
			/// <summary>
			/// Function that hooks into the jqgrid api to provided decesion 
			/// behavior for routing format requests to the appropriate format stategy function.
			/// </summary>
			/// <param name="cellValue">the value of the cell</param>
			/// <param name="options">The column model</param>
			/// <param name="rowObject">The row data object.</param>
			/// <returns type="String">The formatted value.</returns>

			var column = options.colModel;
			var unformattedValue = $(cellValue).html();

			if (column.render && MvcJqGrid.Renderers[column.render]
              && MvcJqGrid.Renderers[column.render].unformat) {

				unformattedValue = MvcJqGrid.Renderers[column.render]
                    .unformat(cellText, column, unformattedValue);
			}

			if (column.formatInfo
                && column.formatInfo.type
                && MvcJqGrid.Formatters[column.formatInfo.type]
                && MvcJqGrid.Formatters[column.formatInfo.type].unformat) {

				unformattedValue = MvcJqGrid.Formatters[column.formatInfo.type]
                    .unformat(column.formatInfo.formatString, unformattedValue);
			}

			return unformattedValue || "";

		},
		RegisterDataFormatter: function (key, formatDelegate, unformatDelegate) {
			/// <summary>
			/// Registers a cell format delegate to be used in the grid system.
			/// </summary>
			/// <param name="key">The key for looking up the format delegate.</param>
			/// <param name="formatDelegate" type="Function">The function(format, value) 
			/// that supplies the Formatting behavior. </param>          
			/// <param name="unformatDelegate" type="Function" optional="true">The function(format, value) 
			/// that supplies the unformatting behavior. </param>

			MvcJqGrid.Formatters[key] = {
				key: key,
				format: formatDelegate,
				unformat: unformatDelegate || function (format, value) { return value; }
			};
		},
		RegisterRenderBehavior: function (key, renderBehavior, unformatDelegate) {
			/// <summary>
			/// Registers a cell renderer delegate to be used in the grid system.
			/// </summary>
			/// <param name="key">The key for looking up the format delegate.</param>
			/// <param name="formatDelegate" type="Function">The function(cellvalue, column, rowObject) that supplies the Formatting behavior. </param>
			/// <param name="unformatDelegate" type="Function" optional="true">The function(cellText, column, cellValue) that supplies the unformatting behavior. </param>

			MvcJqGrid.Renderers[key] = {
				key: key,
				behavior: renderBehavior,
				unformat: unformatDelegate || function (cellvalue, column, rowObject) { return cellvalue; }
			};
		},
		RegisterEditor: function (key, initializeDataDelegate, createElementDelegate, getValueDelegate) {
			/// <summary>
			/// Registers a Editor Control given the required and optional delegates. Note, the createElement and getValue delegates
			/// are optional but both are required if one implemented.
			/// </summary>
			/// <param name="key"></param>
			/// <param name="initializeDataDelegate" type="Function" >A function(elem) that initializes the the html to make
			///  it a javascript enabled control. e.g bootstrapping functionallity. </param>
			/// <param name="createElementDelegate" type="Function" optional="true"> A function(value, options) that returns a jQuery wrapped 
			/// element for the grid to use as a editor control. </param>           
			/// <param name="getValueDelegate" type="Function" optional="true">A function(elem, operation, value)
			///  that provdes the ability for the grid to get the data value from the control.</param>

			MvcJqGrid.Editors[key] = {
				key: key,
				createElement: createElementDelegate,
				initData: initializeDataDelegate,
				getValue: getValueDelegate,
				getColumnEditProperties: function () {
					return {
						edittype: (createElementDelegate !== undefined) ? "custom" : undefined,
						editoptions: {
							custom_element: this.createElement,
							dataInit: this.initData,
							custom_value: this.getValue
						}
					};

				}
			};
		},
		RegisterBuiltinEditor: function (key, edittype) {
			/// <summary>
			/// This function is used to register jqGrid Built in Editor, it is for internal use only.
			/// </summary>
			/// <param name="key"></param>
			/// <param name="editorType"></param>

			MvcJqGrid.Editors[key] = {
				key: key,
				createElement: null,
				initData: null,
				getValue: null,
				getColumnEditProperties: function () {
					return {
						edittype: edittype
					};
				}
			};

		},
		RegisterEditorEventHandler: function (key, eventName, eventDelegate) {
			/// <summary>
			/// Registers a cell renderer delegate to be used in the grid system.
			/// </summary>
			/// <param name="key">The key for looking up the format delegate.</param>
			/// <param name="formatDelegate" type="Function">The function(cellvalue, column, rowObject) 
			/// that supplies the Formatting behavior. </param>
			/// <param name="unformatDelegate" type="Function" optional="true">The function(cellText, column, cellValue) 
			/// that supplies the unformatting behavior. </param>

			MvcJqGrid.EditorEventHandlers[key] = {
				key: key,
				eventName: eventName,
				behavior: eventDelegate
			};
		},
		RegisterEditorColumnRule: function (key, ruleDelegate) {
			/// <summary>
			/// 
			/// </summary>
			/// <param name="key">The key for looking up the rule delegate.</param>
			/// <param name="ruleDelegate" type="Function">The delegate Function (GridRow row, ColumnDictionary columnDictionary) for applying business rules when edit is enabled. </param>

			MvcJqGrid.RegisteredEditorRules[key] = {
				key: key,
				delegate: ruleDelegate
			};

		},
		AttachRegisteredEditorEvents: function (elem) {
			/// <summary>
			/// Internal use only.  
			/// Shared function for attaching registered events to custom editor controls.
			/// </summary>
			/// <param name="elem" type="jQuery">The target element of the event to be attached.</param>

			var colInfo = elem.data("column");

			if (colInfo === null || colInfo === undefined) {
				throw "Custom grid editor is required to have the column model in the data property name column available during rendering.";
			}
			if (colInfo.editorEventHandler !== null && colInfo.editorEventHandler !== undefined) {
				var handlerInfo = MvcJqGrid.GetEditorEventHandler(colInfo.editorEventHandler);
				if (handlerInfo) {
					if (!handlerInfo.behavior) {
						throw "Registered event handler with key " + handlerInfo.key + " had a null event handler.";
					}
					if (!handlerInfo.eventName) {
						throw "Registered event handler with key " + handlerInfo.key + " must have a valid event name.";
					}

					$(elem).on(handlerInfo.eventName, function (e) {
						/// <summary>
						/// Internal use only.  
						/// Shared function for attaching registered events to custom editor controls.
						/// </summary>
						/// <param name="e" type="Event">The target element of the event to be attached.</param>
						var customEvent = $.Event(handlerInfo.eventName);
						$.extend(customEvent, e);
						customEvent.cancelable = true;

						var rowId = $(elem).parents("tr").prop("id");
						var gridId = $(elem).parents("table").prop("id");
						var grid = MvcJqGrid.GetGridRegistration(gridId);
						var row = grid.getGridRow(rowId);

						var data = $(e.target).data();
						var ui = {
							row: row,
							column: data.column,
							cellvalue: data.cellvalue,
							target: $(e.target),
							cancel: false
						};
						var result = handlerInfo.behavior(customEvent, ui);
						if (result === false) {
							e.stopPropagation();
							e.preventDefault();
						}

						return result;
					});

				}
			}
		},
		GetEditorConfiguration: function (editorType) {
			/// <summary>
			/// Returns the column model configuration for the specified EditorType.
			/// </summary>
			/// <param name="editorType">The MvcJqGrid.EditorTypes type string.</param>

			if (MvcJqGrid.Editors[editorType]) {
				return MvcJqGrid.Editors[editorType].getColumnEditProperties();
			}

			return null;
		},
		RegisterColumnTemplate: function (key, columnTemplateDelegate) {
			/// <summary>
			/// Registers a Column Model Template with the MvcJqGrid system for use.
			/// </summary>
			/// <param name="key"></param>
			/// <param name="columnTemplateDelegate" type="Function">A function() that returns the column Model Template. </param>

			MvcJqGrid.ColumnTemplates[key] = {
				key: key,
				template: columnTemplateDelegate()
			};
		},
		GetColumnTemplate: function (key) {
			/// <summary>
			/// returns the column template matching the specifiec key, otherwise null.
			/// </summary>
			/// <param name="key">The column template key.</param>

			if (MvcJqGrid.ColumnTemplates[key]) {
				return MvcJqGrid.ColumnTemplates[key].template || null;
			}

			return null;
		},
		GetRenderBehavior: function (key) {
			/// <summary>
			/// Returns the Render Behavior matching the specified key.
			/// </summary>
			/// <param name="key"></param>
			/// <returns type="Function"></returns>

			if (MvcJqGrid.Renderers[key]) {
				return MvcJqGrid.Renderers[key].behavior || null;
			}

			return null;
		},
		GetEditorEventHandler: function (key) {
			/// <summary>
			/// Returns the Render Behavior matching the specified key.
			/// </summary>
			/// <param name="key"></param>
			/// <returns type="Function"></returns>

			if (MvcJqGrid.EditorEventHandlers[key]) {
				return MvcJqGrid.EditorEventHandlers[key] || null;
			}

			return null;
		},
		RunRule: function (key, gridRows, columnDictionary, extra) {
			/// <summary>
			/// 
			/// </summary>
			/// <param name="key"></param>
			/// <param name="gridRow"></param>
			/// <param name="columnDictionary"></param>
			/// <param name="extra" optional="true"></param>

			var registedRule = MvcJqGrid.RegisteredEditorRules[key];
			if (registedRule) {
				return registedRule.delegate(gridRows, columnDictionary, extra);
			}

			return true;


		},
		GetGridRegistration: function (selector) {
			return MvcJqGrid.grids[selector];
		}
	};

	//#endregion

	//#region Buit-in Data Formatters

	MvcJqGrid.RegisterDataFormatter(MvcJqGrid.DataFormatters.None, function (format, value) {
		return value;
	});

	MvcJqGrid.RegisterDataFormatter(MvcJqGrid.DataFormatters.Date, function (format, value) {
		if (format) {
			var cellvalue = new Date(parseInt(value.substr(6), 10))
                .toString(format);
			if (cellvalue === '1-01-01' || cellvalue === '0-12-31' || cellvalue === '0001-01-01') {
				return "";
			}
			return cellvalue;
		}

		return value;
	});

	//
	MvcJqGrid.RegisterDataFormatter(MvcJqGrid.DataFormatters.Number, function (format, value) {
		return value;
	});

	//#endregion

	//#region Built-in Render Behaviors

	MvcJqGrid.RegisterRenderBehavior(MvcJqGrid.RenderBehavior.Link, function (cellvalue, column, rowObject) {
		/// <summary>
		/// Provides format rendering stategy for rendering links.
		/// </summary>
		/// <param name="cellvalue">the unformated cell value.</param>
		/// <param name="column">The column model</param>
		/// <param name="rowObject">the row's data oejct.</param>
		/// <returns type="">The formated cell value.</returns>

		var href = "";

		if (!cellvalue) {
			return '';
		}

		if (column.url) {
			href = String.formatTokens(column.url, rowObject);

		}
		else if (column.urlProperty && rowObject[column.urlProperty]) {
			href = rowObject[column.urlProperty];
		}

		if (href === "") {
			return cellvalue;
		}

		var a = $(document.createElement('a'));
		a.prop('href', href)
            .text(cellvalue);
		return a.htmlAll();
	});

	MvcJqGrid.RegisterRenderBehavior(MvcJqGrid.RenderBehavior.Default, function (cellvalue, column, rowObject) {
		/// <summary>
		/// Provides format rendering stategy for rendering links.
		/// </summary>
		/// <param name="cellvalue">the unformated cell value.</param>
		/// <param name="column">The column model</param>
		/// <param name="rowObject">the row's data oejct.</param>
		/// <returns type="String">The formated cell value.</returns>

		if (cellvalue && cellvalue instanceof Object) {
			var wrapper = $(document.createElement('div'))
                .css("display", "inline-block")
                .attr("data-object", JSON.stringify(cellvalue))
                .text(cellvalue);

			return wrapper.htmlAll();
		}
		return cellvalue;

	},
        function (celltext, column, cellValue) {
        	var result = $(cellValue).data("object");
        	if (result) {
        		return result;
        	}
        	return cellValue;
        });

	MvcJqGrid.RegisterRenderBehavior(MvcJqGrid.RenderBehavior.CellMarker, function (cellvalue, column, rowObject) {
		/// <summary>
		/// Provides format rendering stategy for rendering links.
		/// </summary>
		/// <param name="cellvalue">the unformated cell value.</param>
		/// <param name="column">The column model</param>
		/// <param name="rowObject">the row's data oejct.</param>
		/// <returns type="String">The formated cell value.</returns>

		var wrapper = $(document.createElement("div"))
            .addClass("grid-status-marker")
            .addClass(column.name.toLowerCase() + "-" + cellvalue.toLowerCase().trim().replace(/\s+/g, "-"))
            .text(cellvalue);

		return wrapper.htmlAll();
	});

	MvcJqGrid.RegisterRenderBehavior(MvcJqGrid.RenderBehavior.EditActions, function (cellvalue, column, rowObject) {

		/// <summary>
		/// Renders the edit actions buttons 
		/// </summary>
		/// <param name="cellvalue"></param>
		/// <param name="column"></param>
		/// <param name="rowObject"></param>

		//edit button
		var btn = $(document.createElement("div"))
            .attr("style", "height:17px;width:18px;")
            .attr("data-row-id", rowObject.Id)
            .attr("type", "button")
            .attr("class", "grid-row-edit ui-pg-div ui-inline-edit")
            .attr("onmouseover", "jQuery(this).addClass('ui-state-hover'); ")
            .attr("onmouseout", "jQuery(this).removeClass('ui-state-hover');")
            .append($(document.createElement("span"))
                .attr("class", "ui-icon ui-icon-pencil")
                .attr("title", "Edit"));

		var buttons = '<div style="display:inline-block">' + btn.htmlAll() + '</div>';

		//delete button
		btn.attr("class", "grid-row-delete ui-pg-div ui-inline-delete")
                 .find("span")
                 .attr("class", "ui-icon ui-icon-trash")
                 .attr("title", "Delete");

		buttons += '<div style="display:inline-block">' + btn.htmlAll() + '</div>';

		//save button
		btn.attr("class", "grid-row-save ui-pg-div ui-inline-save").hide()
            .find("span")
            .attr("class", "ui-icon ui-icon-disk")
            .attr("title", "Save");

		buttons += '<div style="display:inline-block">' + btn.htmlAll() + '</div>';

		//cancel button
		btn.attr("class", "grid-row-cancel ui-pg-div ui-inline-cancel").hide()
             .find("span")
            .attr("class", "ui-icon ui-icon-cancel")
            .attr("title", "Cancel");

		buttons += '<div style="display:inline-block">' + btn.htmlAll() + '</div>';

		return '<div style="white-space:nowrap;">' + buttons + "</div";
	});

	MvcJqGrid.RegisterRenderBehavior(MvcJqGrid.RenderBehavior.ValidationSummary, function (cellvalue, column, rowObject) {
		var func = MvcJqGrid.GetRenderBehavior(MvcJqGrid.RenderBehavior.Icons);

		cellvalue = {
			items: [{
				Link: "#",
				ImageClass: "icon icon-error",
				Classes: "error",
				Enabled: false,
				Data: null,
				TooltipRender: MvcJqGrid.RenderBehavior.ValidationTooltip
			}]
		};

		return func(cellvalue.items, column, rowObject);
	});

	MvcJqGrid.RegisterRenderBehavior(MvcJqGrid.RenderBehavior.ValidationTooltip, function (cellvalue, column, rowObject) {
		/// <summary>
		/// 
		/// </summary>
		/// <param name="cellvalue">An array of server validation error objects</param>
		/// <param name="column"></param>
		/// <param name="rowObject"></param>

		var wrapper = $(document.createElement("ul")).width(400);
		Enumerable.From(cellvalue).ForEach(function (error) {

			var targetColumnNames = Enumerable.From(error.TargetProperties)
                .Select(function (x) { return x.DisplayName; })
                .ToArray().join(", ");

			wrapper.append($(document.createElement("li"))
                .append($(document.createElement("div"))
                    .addClass("error-field-names")
                    .text(targetColumnNames)
                )
                .append($(document.createElement("div"))
                    .addClass("error-message")
                    .text(error.Message)
                )
           );
		});
		return wrapper.htmlAll();
	});

	MvcJqGrid.RegisterRenderBehavior(MvcJqGrid.RenderBehavior.SimpleTooltip, function (cellvalue, column, rowObject) {
		cellvalue = cellvalue || "";
		return $(document.createElement("div")).text(cellvalue).htmlAll();
	});

	MvcJqGrid.RegisterRenderBehavior(MvcJqGrid.RenderBehavior.ListTooltip, function (cellvalue, column, rowObject) {
		var wrapper = $(document.createElement("ul"));
		Enumerable.From(cellvalue).ForEach(function (x) {
			wrapper.append($(document.createElement("li"))
                .text(x));
		});
		return wrapper.htmlAll();
	});

	MvcJqGrid.RegisterRenderBehavior(MvcJqGrid.RenderBehavior.Icons, function (cellvalue, column, rowObject) {
		/// <summary>
		/// Render Behavior for an Icons Column.
		/// </summary>
		/// <param name="cellvalue" "String"></param>
		/// <param name="column"></param>
		/// <param name="rowObject"></param>

		var wrapper = $(document.createElement('div'))
            .css("display", "inline-block")
            .attr("data-object", JSON.stringify(cellvalue));

		Enumerable.From(cellvalue).ForEach(function (icon) {

			var toolTipRender = MvcJqGrid.GetRenderBehavior(icon.TooltipRender)
                || MvcJqGrid.GetRenderBehavior(MvcJqGrid.RenderBehavior.SimpleTooltip);

			var div = $(document.createElement('div'))
                .css("display", "inline-block")
                .append($(document.createElement('div'))
                    .addClass('dropdown hover-menu')
                    .addClass(icon.Classes)
                    .addClass((icon.Enabled) ? "enabled" : "")
                    .addClass('disableInEdit')
                    .append($(document.createElement('a'))
                        .addClass('dropdown-toggle')
                        .addClass(icon.ImageClass)
                        .addClass((icon.Enabled) ? "enabled" : "")
                        .prop('href', (icon.Link !== null) ? icon.Link : "#")
                        .prop('target', (icon.LinkTarget !== undefined && icon.LinkTarget !== null && icon.LinkTarget.toLowerCase() !== "modal") ? icon.LinkTarget : "_self")
                        .attr('data-toggle', (icon.LinkTarget !== undefined && icon.LinkTarget !== null && icon.LinkTarget.toLowerCase() === "modal") ? 'modal' : "")
                        .text(" ")
                    )
                    .append($(document.createElement("div"))
                        .addClass("dropdown-menu")
                        .css("text-align", "left")
                        .attr('role', "tooltip")
                        .html((toolTipRender !== undefined && toolTipRender !== null)
                        ? toolTipRender(icon.Data, column, rowObject) : "")
                    )
                );
			

			wrapper.append(div);
		});

		return wrapper.htmlAll();
	},
    function (celltext, column, cellValue) {
    	/// <summary>
    	/// the Icon unformat function needed if cell goes into edit mode.
    	/// </summary>
    	/// <param name="cellValue"></param>
    	/// <param name="column"></param>
    	/// <param name="rowObject"></param>
    	/// <returns type="Icons"></returns>

    	return $(cellValue).data("object");

    });

	MvcJqGrid.RegisterRenderBehavior(MvcJqGrid.RenderBehavior.DropDown,
        function (cellvalue, column, rowObject) {
        	var cellData = cellvalue.SelectedDisplay || "";
        	var wrapper = $(document.createElement("div"))
                .addClass("grid-status-marker")
                .addClass(column.name.toLowerCase() + "-" + cellvalue.SelectedValue.toLowerCase().trim().replace(/\s+/g, "-"))
                .attr("data-select-info", JSON.stringify(cellvalue))
                .text(cellData);

        	return wrapper.htmlAll();
        },
        function (celltext, column, cellValue) {

        	return $(cellValue).data("selectInfo");
        });

	MvcJqGrid.RegisterRenderBehavior(MvcJqGrid.RenderBehavior.ActionMenu,
        function (cellvalue, column, rowObject) {

            var wrapper = $(document.createElement("div"))
               .css("display", "inline-block")
               .addClass("result-actions")
               .attr("data-object", JSON.stringify(cellvalue));

        	var menuLinks = cellvalue.ActionMenuLinks || null;

        	if (menuLinks && menuLinks.length > 0) {

        	    var ul = $(document.createElement('ul'))
                       .addClass("dropdown-menu")
                       .attr('role', "menu");

	            var div = $(document.createElement('div'))
	                .css("display", "inline-block")
	                .append($(document.createElement('div'))
	                    .addClass('dropdown')
	                    .addClass('disableInEdit')
	                    .append($(document.createElement('a'))
	                        .addClass('dropdown-toggle caret')
	                        .attr('data-toggle', "dropdown")
	                        .prop('href', '#')
	                        .text(" "))
	                    .append(ul)
                    );

        		Enumerable.From(menuLinks).ForEach(function (item) {
        			var includeTarget = item.target === 'function' ? false : true;
        			var li = $(document.createElement('li'));
        			var anchor = $(document.createElement('a'));

        			anchor.addClass(item.Classes)
                            .addClass((item.Enabled) ? "enabled" : "")
                        .attr("data-row-id", rowObject.Id)
                        .prop('href', (item.Link !== null) ? item.Link : "#");

        			if (includeTarget) {
        				anchor.prop('target', (item.LinkTarget !== undefined && item.LinkTarget !== null
                            && item.LinkTarget.toLowerCase() !== "modal") ? item.LinkTarget : "_self");
        				anchor.attr('data-toggle', (item.LinkTarget !== undefined && item.LinkTarget !== null
                            && item.LinkTarget.toLowerCase() === "modal") ? 'modal' : "");
        			}
        			anchor.text(item.Title);
        			li.append(anchor);
        			ul.append(li);
        		});

        		wrapper.append(div);

        	}

        	return wrapper.htmlAll();
        },
        function (celltext, column, cellValue) {

        	return $(cellValue).data("object");
        });

	///#endregion

	//#region Built-in Editor Controls
	MvcJqGrid.RegisterEditor(MvcJqGrid.EditorTypes.AutoComplete,
        function (elem) {
        	/// <summary>
        	/// Initialize/boot strap AutoComplete Editor Control
        	/// </summary>
        	/// <param name="elem"></param>
        	var autoComplete = $($(elem).find("input"));
        	MvcJqGrid.AttachRegisteredEditorEvents(autoComplete);

        	setTimeout(function () {
        		$(elem).find(".ul-autocomplete").ulAutoComplete();
        	}, 50);
        },
        function (value, options) {
        	/// <summary>
        	/// Autocomplete Editor Create Element
        	/// </summary>
        	/// <param name="value"></param>
        	/// <param name="options"></param>

        	var columns = Enumerable.From($(this).data("columnModels"));
        	var colInfo = columns.FirstOrDefault(null, function (x) { return x.name === options.name; });
        	var editorInfo = colInfo.customEditorInfo;
        	if (editorInfo !== null) {

        		var elem = $(document.createElement("input"))
                       .data("column", colInfo)
                       .data("cellvalue", value);

        		elem.addClass("ul-autocomplete");
        		elem.attr("data-url", editorInfo.url);
        		elem.attr("data-min-term-length", editorInfo.minSearchTermLength);
        		elem.attr("data-max-scroll-items", editorInfo.maxListSize);
        		elem.attr("data-id-member", editorInfo.listIdMember);
        		elem.attr("data-display-member", editorInfo.listDisplayMember);
        		elem.attr("data-description-member", editorInfo.listDescriptionMember);
        		elem.val(value);

        		return elem;
        	}

        	return null;

        },
        function (elem, operation, value) {
        	/// <summary>
        	/// AutoComplete GetValue from Control Function
        	/// </summary>
        	/// <param name="elem"></param>
        	/// <param name="operation"></param>
        	/// <param name="value"></param>

        	return $(elem).val();
        });

	MvcJqGrid.RegisterEditor(MvcJqGrid.EditorTypes.DatePicker,
		function (elem) {
			var columns = Enumerable.From($(this).data("columnModels"));

			setTimeout(function () {
				var isReadonly = !($(elem).attr("readonly") === undefined);
				if (!isReadonly) {
					var colInfo = columns.FirstOrDefault(null, function (x) { return x.name === elem.name; });
					$(elem).datepicker({
						format: 'yyyy-mm-dd',
						autoclose: true
					});

					$(elem).data("column", colInfo);
					MvcJqGrid.AttachRegisteredEditorEvents($(elem));
				}
			}, 50);
		});



	MvcJqGrid.RegisterEditor(MvcJqGrid.EditorTypes.InputMask,
		function (elem) {
		    var columns = Enumerable.From($(this).data("columnModels"));
            var colInfo = columns.FirstOrDefault(null, function (x) { return x.name === elem.name; });

            if (!colInfo.inputMask)            {
                window.console.log("When using EditorType.InputMask, the column attribute requires a valid input mask stirng value is required.");
                return;
            }

		    setTimeout(function () {
		        var isReadonly = !($(elem).attr("readonly") === undefined);
		        if (!isReadonly) {
		         
		            $(elem).inputmask(colInfo.inputMask);
		            $(elem).css("width", "90%");
		            $(elem).data("column", colInfo);
		            MvcJqGrid.AttachRegisteredEditorEvents($(elem));
		        }
		    }, 50);
		});


	MvcJqGrid.RegisterEditor(MvcJqGrid.EditorTypes.Default,
			function (elem) {
				/// <summary>
				/// 
				/// </summary>
				/// <param name="elem"></param>
				return;
			},
			function (value, options) {
				/// <summary>
				/// Text Editor Create Element
				/// </summary>
				/// <param name="value"></param>
				/// <param name="options"></param>
				var elem = $(document.createElement("input"))
					.prop("id", options.id)
					.prop("name", options.name)
					.prop("role", "textbox")
					.attr("style", "width: 98%")
					.addClass("editable")
					.val(value.htmlDecode());

				return elem;
			},
			function (elem, operation, value) {
				/// <summary>
				/// Text GetValue from Control Function
				/// </summary>
				/// <param name="elem"></param>
				/// <param name="operation"></param>
				/// <param name="value"></param>

				return $(elem).val().htmlEncode();
			});

	MvcJqGrid.RegisterBuiltinEditor(MvcJqGrid.EditorTypes.Checkbox, "checkbox");

	MvcJqGrid.RegisterEditor(MvcJqGrid.EditorTypes.DropDown,
		function (elem) {
			var select = $($(elem).find("select"));
			MvcJqGrid.AttachRegisteredEditorEvents(select);
		},
		function (value, options) {
			var columns = Enumerable.From($(this).data("columnModels"));
			var colInfo = columns.FirstOrDefault(null, function (x) { return x.name === options.name; });

			var elem = $(document.createElement("select"))
				.data("column", colInfo)
				.data("cellvalue", value);

			elem.attr("style", "width: 98%; margin: 0px");

			Enumerable.From(value.Data).ForEach(function (item) {
				var option = $(document.createElement("option"))
					//.attr("value", item.DisplayIdMember)
					.val(item.DisplayIdMember)
					.text(item.DisplayMember);

				var selectedValue = $(elem).data("cellvalue").SelectedValue;
				if (selectedValue === item.DisplayIdMember) {
					$(option).attr("selected", "selected");
				}

				elem.append(option);
			});

			return elem;
		},
		function (elem, operation, value) {
			var data = $(elem).data();
			data.cellvalue.SelectedValue = $(elem).val();
			return $(elem).data("cellvalue");

		});

	//#endregion

	//#region Built-in Column Templates

	MvcJqGrid.RegisterColumnTemplate(MvcJqGrid.ColumnTemplateTypes.ActionMenu, function () {
		return {
			sortable: false,
			align: 'center',
			width: 35,
			fixed: true,
			resizable: false,
			title: false,
			classes: "embed-action-dropdown"
		};
	});

	MvcJqGrid.RegisterColumnTemplate(MvcJqGrid.ColumnTemplateTypes.Icons, function () {
		return {
			sortable: false,
			align: 'center',
			width: 40,
			fixed: true,
			resizable: false,
			title: false,
			classes: "embed-action-dropdown"
		};
	});

	//#endregion



	//Allow Global access to the MvcGrid
	//Custom formatting and rendering static api.
	UL.MvcJqGrid = MvcJqGrid;


	//#region Grid State Class

	var GridState = function (grid) {
		/// <summary>
		/// Provides a state object form maintaining runtime information for a specific grid instance.
		/// </summary>
		/// <param name="grid" type="jQuery">a jqResult reference to the specific grid being tracked by this state instnace.</param>
		///<field name="grid" type="jQuery">The jqResult reference to the specified grid.</field>
	    ///<field name="state" type="Object">The object containing grid state information.</field>
	    
	    if (grid && grid.addClass === undefined) {
	        throw "Invalid Argument: Parameter 'guid' is required to be a jQuery object reference to the grid html element.";
	    }

	    this.grid = grid;
		this.state = {};
	};

	GridState.prototype = {
		storage: $.initNamespaceStorage('UL.MvcGrid').localStorage,
		save: function () {
		    /// <summary>
		    /// Saves the grid state to local storage.
		    /// </summary>
			var key = "";
			var statekey = "";

			for (key in MvcJqGrid.StateItemKeys) {
			    if (MvcJqGrid.StateItemKeys.hasOwnProperty(key)) {
			        statekey = MvcJqGrid.StateItemKeys[key];
					this.state[key] = this.grid.jqGrid('getGridParam', statekey);
				}
			}

			this.storage.set(this.grid.prop('id'), this.state);

		},
		load: function () {
		    /// <summary>
		    /// Loads the grids properties from local storage to the grid specified at contruction.
		    /// </summary>

			var state = this.storage.get(this.grid.prop('id'));
			var key = "";
			var statekey = "";

			for (key in state) {
				if (state.hasOwnProperty(key)) {
					this.state[key] = state[key];
					statekey = MvcJqGrid.StateItemKeys[key];
					if (statekey && statekey !== MvcJqGrid.StateItemKeys.COLUMNS) {
						this.grid.jqGrid('setGridParam', statekey, this.state[key]);
					}
				}
			}

		}
	};

	//#endregion

	//#region GridRow Classs

	var GridRow = function (grid, rowid) {
		/// <summary>
		/// 
		/// </summary>
		/// <param name="grid"></param>
		/// <param name="rowid"></param>

		/// <field name="savebtn" type="jQuery"></field>
		/// <field name="cancelbtn" type="jQuery"></field>
		/// <field name="editbtn" type="jQuery"></field>
		/// <field name="deletebtn" type="jQuery"></field>
		/// <field name="treebtn" type="jQuery"></field>




		this.Id = function () {
			/// <summary>
			/// Gets the Row & Entity's id/Pk
			/// </summary>
			return rowid;
		};
		this.grid = function () {
			/// <summary>
			/// Gets jQuery reference to the JqGrid target Element 
			/// </summary>
			/// <returns type="jQuery"></returns>
			return grid;
		};

		this.rowVersions = [];
	};
	UL.MvcJqGrid.GridRow = GridRow;
	GridRow.prototype = {
		/// <field name="rowVersion" type="Array"></field>
		rowVersions: [],
		rowData: function (value) {
			/// <summary>
			/// Gets or Sets the row's data.
			/// </summary>
			/// <param name="value" optional="true"></param>

			if (value) {
				this.grid().jqGrid("setRowData", this.Id(), value);
				return value;
			}

			return this.grid().jqGrid("getRowData", this.Id());
		},
		tr: function () {
			/// <summary>
			/// Returns the html table row object wrapped in jQuery.
			/// </summary>
			/// <returns type="jQuery"></returns>

			return $(this.grid().find("tr#" + this.Id()));
		},
		savebtn: function () {
			return this.tr().find(".grid-row-save");
		},
		cancelbtn: function () {
			return this.tr().find(".grid-row-cancel");
		},
		editbtn: function () {
			return this.tr().find(".grid-row-edit");
		},
		deletebtn: function () {
			return this.tr().find(".grid-row-delete");
		},
		treebtn: function () {
			return this.tr().find(".tree-wrap");
		},
		toggleButtons: function (isEditMode) {
			/// <summary>
			/// Toggles button visibility based on state of edit mode.
			/// </summary>
			/// <param name="editMode"></param>

			if (isEditMode) {
				this.editbtn().hide();
				this.deletebtn().hide();
				this.treebtn().hide();
				this.savebtn().show();
				this.cancelbtn().show();
			}
			else {
				this.editbtn().show();
				this.deletebtn().show();
				this.savebtn().hide();
				this.cancelbtn().hide();
				this.treebtn().show();
			}
		},
		edit: function () {

			var self = this;
			var tr = this.tr();
			self.toggleButtons(true);

			if (!this.isDirty()) {

				if (this.rowVersions.length <= 0) {
					this.rowVersions.push(this.rowData());
				}

				tr.find("input, select, textarea").one("change", function (e) {
					self.markDirty();
				});
			}
			else {
				self.markDirty();
			}

		},
		takeDataSnapShot: function () {
			if (!this.isDirty()) {
				this.rowVersions.push(this.rowData());
			}
		},
		save: function (value, mergeIfDirty) {
			/// <summary>
			/// Saves the value to the row, toggles edit buttons, and marks clean.
			/// </summary>
			/// <param name="value"></param>
			/// <param name="mergeIfDirty" type="Boolean" optional="true"></param>

			this.toggleButtons(false);
			this.rowData(value);
			if (this.rowVersions.length > 0) {
			    this.rowVersions = [];
			}
			this.unMarkDirty();
		},
		deleteRow: function () {
			this.grid().jqGrid('delRowData', this.Id());
		},
		commitChangesLocal: function () {
			this.toggleButtons(false);

			this.grid().jqGrid('saveRow', this.Id(), {
				url: 'clientArray'
			});
		},
		restore: function () {

			this.grid().jqGrid('restoreRow', this.Id());
			this.toggleButtons(false);
			if (this.rowVersions.length > 0) {
				this.rowData(this.rowVersions[0]);
				this.rowVersions = [];
			}
			this.unMarkDirty();
            
            
		},
		markDirty: function () {
			this.tr().attr(MvcJqGrid.AttributeNames.isDirty, true);
		},
		unMarkDirty: function () {
			this.tr().removeAttr(MvcJqGrid.AttributeNames.isDirty);
		},
		isDirty: function () {
			return this.tr().attr(MvcJqGrid.AttributeNames.isDirty) === "true";
		},
		inEditMode: function () {
			return this.tr().attr("editable") === "1";
		},
		validationSummary: function (value) {
			/// <summary>
			/// Gets or Sets the validation Summary content.
			/// </summary>
			/// <param name="value" type="String" optional="true"></param>

			var summary = this.tr().find(".error .dropdown-menu");
			if (value) {
				summary.html(value);
			}

			return summary.html();
		},
		showColumnErrors: function (validationErrors) {
			/// <summary>
			/// 
			/// </summary>
			/// <param name="validationErrors"></param>
			var self = this;
			var tr = self.tr();

			//Loop over each valiation error
			Enumerable.From(validationErrors).ForEach(function (error) {
				var targetGroupSelector = "";

				//build selector for all target columns of the error
				Enumerable.From(error.TargetProperties).ForEach(function (columnInfo) {
					targetGroupSelector += self.getInputSelector(columnInfo.Name) + ",";
				});

				if (targetGroupSelector !== "") {
					//remove last comma.
					targetGroupSelector = targetGroupSelector
						.substr(0, targetGroupSelector.length - 1);

					//get all target editable controls.
					tr.find(targetGroupSelector)
						 .addClass("input-validation-error ")
						 .prop("title", error.Message);
				}
			});
		},
		getInputSelector: function (columnName) {
			/// <summary>
			/// Given the columnName, returns the selector for the input field.
			/// </summary>
			/// <param name="columnName"></param>

			return "#" + this.Id() + "_" + columnName;
		},
		enableIcon: function (iconSelector) {
			var iconContainer = this.tr().find(iconSelector);
			iconContainer.addClass("enabled");
			iconContainer.find("a")
				.addClass("enabled");

		},
		disableIcon: function (iconSelector) {
			var iconContainer = this.tr().find(iconSelector);
			iconContainer.removeClass("enabled");
			iconContainer.find("a")
				.removeClass("enabled");
		},
		applyValidationSummary: function (validationErrors) {
			var validationRender = MvcJqGrid.GetRenderBehavior(MvcJqGrid.RenderBehavior.ValidationTooltip);
			this.enableIcon(".error");
			this.validationSummary(validationRender(validationErrors, "vsummary", this.rowData()));
		},
		select: function () {
			var chk = this.tr().find(".grid-row-select input[type='checkbox']");
			if (!chk.is(":checked")) {
				chk.trigger("click");
			}
		},
		unselect: function () {
			this.tr().find(".grid-row-select input[type='checkbox']:checked")
				.trigger("click");
		},
		isSelected : function() {
			return this.tr().find(".grid-row-select input[type='checkbox']:checked") > 0;
		}
	};

	//#endregion

	//#region GridRegistration Class

	var GridRegistration = function (proxy) {
		/// <summary>
		/// Grid Registation Class, this is the main wrapper 
		/// around each instance of a jqGrid.
		/// </summary>
		/// <param name="proxy" type="UL.Proxy">Proxy used for requests to 
		/// server when useManagedDataLoad mode. </param>
		///<field name="grid" type="jQuery"></field>;
		this.selector = null;
		this.pagerSelector = null;
		this.pager = null;
		this.grid = null;
		this.columnModels = null;
		this.columnDictionary = null;
		this.columnNames = null;
		this.element = null;
		this.criteria = null;
		this.pageSize = 10;
		this.addActionColumn = true;
		this.sortName = null;
		this.sortOrder = "asc";
		this.state = null;
		this.enbleState = true;
		this.hasInitStateRan = false;
		this.proxy = proxy;
		this.rowDict = {};
		this.multiSelect = true;
		this.selectRowIds = [];
		this.multiSelectCheckBoxSelector = "";
	};
	UL.MvcJqGrid.GridRegistration = GridRegistration;

	UL.MvcJqGrid.GridRegistration = GridRegistration;
	GridRegistration.prototype = {
		gridRowsDict: {},
		init: function () {
			this.multiSelectCheckBoxSelector = ".grid-row-select input[type='checkbox']";
			this.columnDictionary = Enumerable.From(this.columnModels).ToDictionary(function (x) { return x.name; });
			this.gridDataRules = this.dataRules;
			this.rowHeight = this.rowSize;
			this.editRowHeight = this.editRowSize;
			this.initExtendedActionColumn();
			this.initEditAbility();
			this.configureColumns(this.columnModels);
			//initMultiSelect must happen after configure columns because we want the 
			//grid default for checkboxes to happen.
			this.initMultiSelect();
			this.grid = this.element;
			this.buildGrid();

			return;
		},
		initExtendedActionColumn: function () {
			/// <summary>
			/// If add action column is enabled, 
			/// then initialize the extended action column.
			/// </summary>
			if (this.addActionColumn && this.actionMemuRender) {
				this.createActionColumn(this.actionMemuRender);
			}
		},
		initEditAbility: function () {
			/// <summary>
			/// If editing is enabled, initialize edit features.
			/// </summary>

			if (this.isEditable) {
				this.createEditActionsColumn();

				if (this.includeValidationSummary) {
					this.createValidationSummaryColumn();
				}
			}
		},
		initMultiSelect: function () {
			if (this.multiSelect === true) {
				this.createMultiSelectCheckboxColumn();
			}
		},
		createMultiSelectCheckboxColumn: function () {

			this.columnModels.insert(0, {
				name: "selected",
				index: 'selected',
				classes: 'grid-row-select',
				width: 20,
				align: 'center',
				formatter: 'checkbox',
				editoptions: { value: '1:0' },
				formatoptions: {
					disabled: false
				}
			});

			this.columnNames.insert(0, "");
		},
		calculateHeightAboveFold: function () {
			/// <summary>
			/// Calculates the maximum height of the grid 
			/// before the page scrollbars would be needed.
			/// </summary>

			var bodyRect = document.body.getBoundingClientRect(),
				  elemRect = this.element[0].getBoundingClientRect(),
				  offset = elemRect.top - bodyRect.top;
			return $(window).height() - offset - 70;
		},
		getTreeColumnName: function () {
			/// <summary>
			/// If configured, returns the column name of the column
			/// designated as tree structure, otherwise returns null.
			/// </summary>
			///<returns type="String"></returns>

			return Enumerable.From(this.columnModels)
				 .Where(function (x) { return x.isTreeColumn; })
				 .Select(function (x) { return x.name; })
				 .FirstOrDefault(null);
		},
		buildGrid: function () {
			/// <summary>
			/// 
			/// </summary>
			/// <param name="registration" type="GridRegistration"></param>

			var self = this;
			var gridHeight = self.height;
			if (self.fillFoldHeight) {
				gridHeight = self.calculateHeightAboveFold();
			}

			var treeColumnName = self.getTreeColumnName();


			self.grid.jqGrid({
				url: (self.useManagedDataLoad) ? null : self.url,
				datatype: (self.useManagedDataLoad) ? "jsonstring" : "json",
				datastr: "",
				height: gridHeight,
				width: self.width,
				autowidth: self.autoWidth,
				colNames: self.columnNames,
				colModel: self.columnModels,
				rowNum: self.pageSize,
				rowList: [10],

				scroll: (self.virtualScroll) ? 1 : false,
				scrollerbar: self.virtualScroll,
				loadonce: self.isTreeGrid,
				mtype: "POST",
				rownumbers: false,
				gridview: true,
				treeGrid: self.isTreeGrid,
				ExpandColumn: treeColumnName,
				treeGridModel: self.treeDataFormat,
				treeIcons: {
					plus: 'ui-icon-plusthick',
					minus: 'ui-icon-minusthick',
					leaf: 'ui-icon-blank' //'ui-icon-radio-on'
				},
				viewrecords: true,
				sortname: self.sortName,
				sortorder: self.sortOrder,
				jsonReader: {
					root: "rows",
					page: "page",
					total: "total",
					records: "records",
					repeatitems: true,
					cell: "cell",
					id: "id",
					userdata: "userdata",
					subgrid: {
						root: "rows",
						repeatitems: true,
						cell: "cell"
					}
				},
				resizeStop: function (newWidth, index) {

					if (self.enbleState && self.state) {
						self.state.save();
					}
				},
				beforeRequest: function () {

					self.loadLocalGridState();

					return true;
				},
				beforeSelectRow: function () {

					//only way to stop row selection is to return false 
					//from this event delegate.

					//return self.isEditable;
					return false;
				},
				onSortCol: function (index, iCol, sortorder) {
				 
					var ulOrder = (sortorder === 'asc') ? 'Ascending' : 'Descending';
					self.state.state.sortInfo = {
						field: index,
						order: ulOrder
					};
					self.state.save();

					self.grid.trigger("sorted.mvcjqGrid", [{
						gridInfo: self,
						sortInfo: {
							field: index,
							order: ulOrder
						}
					}]);					
				},
				loadComplete: function () {

					//the status-marker code needs evaluated to be considered generalized code, if not needs removed.
					//may need to expose this event extrnal or use the "classes" member of the column model.
					var jqresult = $(".status-marker").parents("td:not(.status-cell)");
					jqresult.addClass("status-cell");

					$.ajaxSetup({
						contentType: "application/x-www-form-urlencoded; charset=UTF-8"
					});
					self.resizeHeight();
				},
				gridComplete: function () {
					self.registerEditRowEvents();
				},
				serializeGridData: function (postData) {

					$.ajaxSetup({
						contentType: "application/json; charset=utf-8"
					});

					var searchCriteria = self.getSearchCriteria();
					return JSON.stringify({ criteria: searchCriteria, postData: postData });
				},
				ondblClickRow: function (rowid) {
					self.getGridRow(rowid).editbtn().trigger("click");
				}

			});

			if (self.useManagedDataLoad) {
				self.retrieveData();
			}


			self.initHeaderTooltips();

			self.resizeGrid();
			self.registerEditAllButton();
			self.registerCancelAllButton();
			self.registerSaveAllButton();

			return this;
		},
		getGridPostData: function () {
			return this.grid.getGridParam("postData");
		},
		loadLocalGridState: function () {
			//loads the jqgrids grid state if it exists before data is retrieved from server
			if (this.enbleState && !this.hasInitStateRan) {
				var state = new GridState(this.element);
				this.state = state;
				state.load();
				this.hasInitStateRan = true;
			}
		},
		retrieveData: function () {
			/// <summary>
			/// 
			/// </summary>
			/// <param name="registration" type="GridRegistration"></param>

			var self = this;
			var searchCriteria = this.getSearchCriteria();
			var requestModel = {
				criteria: searchCriteria,
				postData: self.getGridPostData()
			};

			this.proxy.send(requestModel, this.url,
				function (success, data, errorInfo) {
					if (success) {
						self.loadGridData(data.rows);
					}
				});
		},
		loadGridData: function (data) {
			/// <summary>
			/// Loads the supplied data array into the specified registatrions's grid.
			/// </summary>
			/// <param name="registration" type="GridRegistration"></param>
			/// <param name="data" type="Array"></param>

			this.grid.jqGrid('setGridParam', {
				datatype: 'local',
				datastr: JSON.stringify(data),
				rowNum: data.length
			}).trigger("reloadGrid");
		},
		resizeGrid: function () {
			/// <summary>
			/// 
			/// </summary>
			/// <param name="registration"></param>
			var self = this;
			var grid = this.grid;

			if (this.snapToParent !== "") {
				var targetParent = grid.parents(this.snapToParent);
				if (this.autoWidth && targetParent.length > 0) {

					//do not delete, this is proper resize calculation
					//$(window).bind('resize', function () {
					//    var parentWidth = targetParent.width();
					//    var calculatedWith = parentWidth;
					//    grid.setGridWidth(calculatedWith, true);
					//}).trigger('resize');

					//this is a workaround due to html flow of flex.
					$(window).bind('resize', function () {
						var newwidth = $(window).width();
						if (newwidth > (targetParent.offset().left + 18)) {
							newwidth = newwidth - (targetParent.offset().left + 18);
						}
						grid.setGridWidth(newwidth, true);
						self.resizeHeight();
					}).trigger('resize');
				}
			}
		},
		resizeHeight: function () {
			/// <summary>
			/// Resizes the grid height based on the 
			/// total of all loaded rows and header heights
			/// </summary>

			if (!this.virtualScroll) {
				var self = this;
				var rows = this.grid.jqGrid('getDataIDs');
				var maxHeight = 0;

				this.grid.find("tr:not('.jqgfirstrow')").each(function (index, element) {
					var currentRow = $(element);
					var currentHeight = currentRow.height();
					var newHeight = (self.isEditable) ? self.editRowHeight : self.rowHeight;

					if (currentHeight !== newHeight) {
						currentRow.height(newHeight);
					}

					var rowH = currentRow.height();
					if (rowH > maxHeight) {
						maxHeight = rowH;
					}
				});

				var gridBodyHeight = rows.length * maxHeight;

				var headerHeight = this.grid.find("tr[role=rowheader]").height();
				var newHeight = gridBodyHeight + headerHeight + 200;
				this.grid.setGridHeight(newHeight, true);
			}
		},
		getEditRowOptions: function () {

			return {
				keys: true,
				url: "clientArray",
				extraparam: null,
				oneditfunc: this.getOnEditHandler(),
				aftersavefunc: this.getAfterSaveHandler(),
				afterrestorefunc: this.getAfterRestoreHandler()
			};
		},
		getOnEditHandler: function () {
			var self = this;
			return function (rowid) {
				var row = self.getGridRow(rowid);
				row.edit();
				var args = { row: row };
				self.raiseEditRowEvent(args);

				var editModeRules = Enumerable.From(self.gridDataRules).Where(function (x) { return x.mode === MvcJqGrid.EditorRuleModes.Edit; });

				editModeRules.ForEach(function (editRule) {
					MvcJqGrid.RunRule(editRule.dataKey, row, self.columnDictionary);
				});

				row.tr().find(".disableInEdit a").each(function (index, elem) {
					self.blockIcon($(elem));
				});

				self.grid.trigger("onRowEditComplete.MvcJqGrid", row);

			};
		},
		disableModals: function () {
			var self = this;
			var jqResults = this.grid.find("tr").not("[editable=1]")
				.find(".disableInEdit a:not([data-url-temp])");

			jqResults.each(function (index, elem) {
				self.blockIcon($(elem));
			});


		},
		assureModalEditablity: function (row) {
			/// <summary>
			/// 
			/// </summary>
			/// <param name="row" optional="true">single row or an array of rows</param>
			var self = this;
			if (!MvcJqGrid.IsInEditMode()) {
				this.grid.find(".disableInEdit a[data-url-temp]").each(function (index, elem) {
					self.unblockIcon($(elem));
				});
			}
			else if (row === undefined || (row && self.isArray(row)) || row instanceof GridRow) {
				this.grid.find(".disableInEdit a:not([data-url-temp])").each(function (index, elem) {
					self.blockIcon($(elem));
				});
			}
		},
        assureMultiselectAvailablity: function() {
            var self = this;
            if (!MvcJqGrid.IsInEditMode()) {
                self.showColumn("selected");
            }
            else {
                self.hideColumn("selected");
                this.grid.find(".grid-row-select input[type='checkbox']:checked")
				.trigger("click");
            }
        },
		isArray: function (target) {
			return (Array.isArray && Array.isArray(target))
				|| Object.prototype.toString.call(target) === '[object Array]';
		},
		unblockIcon: function (anchor) {
			/// <summary>
			/// 
			/// </summary>
			/// <param name="anchor" type="jQuery"></param>

			var tempUrl = anchor.attr("data-url-temp");
			if (tempUrl && tempUrl.length > 0) {
				anchor.attr("href", tempUrl);
			}

			var tempTarget = anchor.attr("data-temp-target");
			if (tempTarget && tempTarget.length > 0) {
				anchor.attr("data-target", tempTarget);
			}

		    var tempToggle = anchor.attr("data-toggle-temp");
			if (tempToggle && tempToggle.length > 0) {
			    anchor.attr("data-toggle", tempToggle);
			}

			anchor.removeAttr("data-temp-target");
			anchor.removeAttr("data-url-temp");
		    anchor.removeAttr("data-toggle-temp");
		},
		blockIcon: function (anchor) {

			anchor.attr("data-url-temp", anchor.attr("href"));
			anchor.attr("href", "#");
			anchor.attr("data-temp-target", anchor.attr("data-target"));
			anchor.attr("data-target", "");
            
			var toggle = anchor.attr("data-toggle");
			if (toggle && toggle.length > 0) {
		        anchor.attr("data-toggle-temp", anchor.attr("data-toggle"));
		        anchor.attr("data-toggle", "");
			    anchor.closest(".dropdown").removeClass("open");
			}


		},
		getAfterRestoreHandler: function () {
			var self = this;

			return function (rowid) {
				var row = self.getGridRow(rowid);
				row.restore();
				self.registerEditRowEvents(rowid);
				self.assureModalEditablity(row);
			    self.assureMultiselectAvailablity();
			};
		},
		getAfterSaveHandler: function () {
			var self = this;
			return function (rowid) {
			    var row = self.getGridRow(rowid);
			    var editOptions = self.getEditRowOptions();

				self.processBeforeSaveRules([row]).done(function (data) {
				    if (data.result) {
				        var returnedRow = data.rows[0];
				        var batchRows = [self.resolveRowData(returnedRow)];

				        self.proxy.send({ request: { Results: batchRows } },
				            self.saveUrl,
				            function(success, dataref, errorDetails) {
				                self.processAfterSave(success, dataref, errorDetails, row);

				            });
				    }
				    else {
				        self.editRow(row.Id(), editOptions);
				    }
				});
			};
		},
		processAfterSave: function (success, dataref, errorDetails, targetRow) {
			var self = this;

			if (success && dataref.success) {

				Enumerable.From(dataref.rows).ForEach(function (x) {
					var row = self.getGridRow(x.Id);
					var temp = x;
					var wasInEditModeAndHadChanges = row.inEditMode() & row.isDirty();
					if ( row.isDirty() && row.Id() !== targetRow.Id() ) {
						row.commitChangesLocal();
						var userChanges = row.rowData();
						temp = self.mergeDirtyObjects(row.rowVersions[0], userChanges, x);
						//make the server copy the new rollback copy.
						row.rowVersions = [x, userChanges];
					}
					else {
						self.restoreRow(x.Id);
					}


					row.save(temp);
					self.registerEditRowEvents(row.Id());
					row.disableIcon(".error");
					if (wasInEditModeAndHadChanges) {
						row.markDirty();
						self.editRow(row.Id());
					}
				});

			}
			else {
				//put back all rows into edit mode
			    this.editRow(targetRow.Id());
				if (dataref) {
					self.applyValidationErrors(dataref.validationErrors);

				}
			}

			self.assureModalEditablity(targetRow);
			self.assureMultiselectAvailablity();
		},
		getSelectedGridRows: function () {
			/// <summary>
			/// Returns an GridRow[]
			/// </summary>
			///<returns type="GridRow[]"></returns>
			var self = this;
			var selectedRows = [];
			Enumerable.From(this.selectRowIds).ForEach(function (id) {
				selectedRows.push(self.getGridRow(id));
			});

			return selectedRows;
		},
		editRow: function (rowid, options) {
			/// <summary>
			/// 
			/// </summary>
			/// <param name="rowid" type="String"></param>
			/// <param name="options" type="gridRowOptions" optional="true"></param>

			var self = this;
			var row = self.getGridRow(rowid);
                
			self.grid.one("onRowEditComplete.MvcJqGrid", function (args) {
				if (!(options && options.editMode === "editAll")) {
				    self.disableModals();
                    self.assureMultiselectAvailablity();
				}

			});

			if (!row.inEditMode()) {
				self.processBeforeEditRules(row).done(function (data) {
				    if (data.result) {
				        row.unselect();
						row.takeDataSnapShot();
						self.grid.jqGrid("editRow", rowid, options || self.getEditRowOptions());
					}
				});
			}
		},
		processMultiAction: function (action, rowData) {
			var self = this;

			var multiActions = {

				multiDelete: function (rowData) {

					var rowsToDelete = self.assureMultiRowDeleteDataFormat(rowData);
					if (rowsToDelete !== null && rowsToDelete.length > 0) {
						self.submitMultiBatchDelete(rowsToDelete);
					}

				},
				multiUpdate: function (rowData) {
					self.submitMultiBatch(rowData);
				}
			};

			if (multiActions[action] !== undefined) {
			    multiActions[action](rowData);
			}
			   
		},
		assureMultiRowDeleteDataFormat: function (rowData) {
			var self = this;
			var exceptionMsg = "Argument Exception: Row data is exptected to be an array of rowdata objects, GridRow objects or string ids";
			if (!self.isArray(rowData)) {
				throw exceptionMsg;
			}

			var dataEnum = Enumerable.From(rowData);
			var rowsToDelete = [];
			if (dataEnum.All(function (x) { return x instanceof GridRow; })) {
				rowsToDelete = dataEnum.Select(function (x) { return x.rowData(); }).ToArray();
			}
			else if (dataEnum.All(function (x) { return typeof x === 'string' || x instanceof String; })) {
				rowsToDelete = dataEnum.Select(function (x) { return self.getGridRow[x].rowData(); }).ToArray();
			}
			else if (dataEnum.All(function (x) { return x !== null && typeof x === 'object'; })) {
				rowsToDelete = rowData;
			}
			else {
				throw exceptionMsg;
			}

			return rowsToDelete;
		},
		processBeforeEditRules: function (row) {
			var self = this;
			var rules = Enumerable.From(self.gridDataRules).Where(function (x) { return x.mode === MvcJqGrid.EditorRuleModes.BeforeEdit; });
			var deferred = $.Deferred();

			if (rules.Count() > 0) {
				rules.ForEach(function (rule) {
					MvcJqGrid.RunRule(rule.dataKey, row, self.columnDictionary)
						.promise()
						.done(function (result) {
							deferred.resolve({ "row": row, "result": result });
						});
				});
			} else {
				deferred.resolve({ "row": row, "result": true });
			}

			return deferred.promise();
		},
		processMultiRowSelectedRules: function (row, selectedInfo) {
			var self = this;
			var rules = Enumerable.From(self.gridDataRules)
				.Where(function (x) { return x.mode === MvcJqGrid.EditorRuleModes.MultiRowSelected; });

			var deferred = $.Deferred();

			if (rules.Count() > 0) {
				rules.ForEach(function (rule) {
					MvcJqGrid.RunRule(rule.dataKey, row, self.columnDictionary, selectedInfo)
						.promise()
						.done(function (result) {
							deferred.resolve(result);
						});
				});
			} else {
				deferred.resolve({
					action: MvcJqGrid.MultiMenuActions.None,
					rowData: Enumerable.From(self.getSelectedGridRows())
						.Select(function (x) { return x.rowData(); })
				});
			}

			return deferred.promise();
		},
		processBeforeDeleteRules: function (row, selectedInfo) {
			var self = this;
			var rules = Enumerable.From(self.gridDataRules)
				.Where(function (x) { return x.mode === MvcJqGrid.EditorRuleModes.BeforeDelete; });

			var deferred = $.Deferred();

			if (rules.Count() > 0) {
				rules.ForEach(function (rule) {
					MvcJqGrid.RunRule(rule.dataKey, row, self.columnDictionary, selectedInfo)
						.promise()
						.done(function (result) {
							deferred.resolve(result);
						});
				});
			} else {

				UL.ShowConfirm("Delete", "Are you Sure you wan to delete this row.", function (result) {

					if (result === true) {
						deferred.resolve({ row: row, result: true });
					}
					else {
						deferred.resolve({ row: row, result: false });
					}
				});
			}

			return deferred.promise();
		},
		registerEditRowEvent: function (handler) {
			this.grid.on("RowEdit.MvcGrid", handler);
		},
		raiseEditRowEvent: function (args) {
			this.grid.trigger("RowEdit.MvcGrid", args);
		},
		saveRow: function (rowid) {
			/// <summary>
			/// 
			/// </summary>
			/// <param name="rowid" type="String"></param>
			var self = this;		
            self.grid.jqGrid('saveRow', rowid, self.getEditRowOptions());			
		},
		removeRow: function (rowid) {
			var self = this;
			var row = self.getGridRow(rowid);
			if (row !== null) {
				self.grid.jqGrid('delRowData', rowid);
				delete this.gridRowsDict[rowid];
			}
		},
		processBeforeSaveRules: function (rows) {
			var self = this;
			var beforeSaveModeRules = Enumerable.From(self.gridDataRules)
				.Where(function (x) { return x.mode === MvcJqGrid.EditorRuleModes.BeforeSave; });

			var deferred = $.Deferred();

			if (beforeSaveModeRules.Count() > 0) {
				beforeSaveModeRules.ForEach(function (rule) {                   
				    MvcJqGrid.RunRule(rule.dataKey, rows, self.columnDictionary)
						.promise()
						.done(function (result) {
							deferred.resolve({ "rows": rows, "result": result });
						});
				});
			} else {
				deferred.resolve({ "row": rows, "result": true });
			}

			return deferred.promise();
		},
		restoreRow: function (rowid) {
			/// <summary>
			/// 
			/// </summary>
			/// <param name="rowid" type="String"></param>

			var self = this;
			var row = self.getGridRow(rowid);
			if (row.inEditMode()) {
				row.restore();
				self.registerEditRowEvents(rowid);
				self.assureModalEditablity(row);
				self.assureMultiselectAvailablity();
			}
		},
		deleteRow: function (rowid) {

			var self = this;
			var row = self.getGridRow(rowid);

			var selectInfo = {
				grid: self,
				rowId: rowid,
				isSelected: true,
				selectedRows: [row]
			};

			self.processBeforeDeleteRules(row, selectInfo).done(function (data) {
				if (data.result) {
					self.submitMultiBatchDelete(data.rowData);
				}
			});

		},
		registerEditRowEvents: function (rowScope) {
			/// <summary>
			/// 
			/// </summary>
			/// <param name="rowScope" type="String" optional="true"></param>

			var self = this;
			rowScope = (rowScope === undefined) ? "" : "tr#" + rowScope + " ";

			self.grid.find(rowScope + ".grid-row-edit").off("click").on("click", function (e, ui) {
				e.preventDefault();
				var rowid = $(this).data().rowId;
				self.editRow(rowid);
			});

			self.grid.find(rowScope + ".grid-row-save").off("click").on("click", function (e, ui) {
				e.preventDefault();
				var rowid = $(this).data().rowId;
				self.saveRow(rowid);
			});

			//row level cancel button
			self.grid.find(rowScope + ".grid-row-cancel").off("click").on("click", function (e, ui) {
				e.preventDefault();
				var rowid = $(this).data().rowId;
				self.restoreRow(rowid);
			});

			//row level cancel button
			self.grid.find(rowScope + ".grid-row-delete").off("click").on("click", function (e, ui) {
				e.preventDefault();
				var rowid = $(this).data().rowId;
				self.deleteRow(rowid);
			});

			//multi-select column cell click event triggers checkbox
			self.grid.find(rowScope + ".grid-row-select").off("click").on("click", function (e, ui) {
				$(this).find('input[type="checkbox"]').trigger("click", ui);
			});

			//multi-select checkbox click event
			self.grid.find(rowScope + this.multiSelectCheckBoxSelector).off("click").on("click", function (e, ui) {

				var rowid = $(this).parents("tr").prop("id");
				var checkbox = $(this);
				if (checkbox) {
					if (checkbox.is(":checked")) {
						if (self.selectRowIds.indexOf(rowid) < 0) {
							self.selectRowIds.push(rowid);
						}
					}
					else {
						self.selectRowIds.remove(rowid);
					}

					self.processMultiRowSelectedRules(self.getGridRow(rowid), {
						grid: self,
						rowId: rowid,
						isSelected: checkbox.is(":checked"),
						selectedRows: self.getSelectedGridRows()
					}).done(function (actionResult) {

						self.processMultiAction(actionResult.action, actionResult.rowData);
						return;
					});
				}

				//must stop propagations so that 
				//td click event doesn't create stack overflow;
				e.stopPropagation();
			});
		},
		registerEditAllButton: function () {
			/// <summary>
			/// 
			/// </summary>

			var self = this;

			if (self.editAllButton !== "") {
				$(self.editAllButton).on("click", function (e, ui) {
					var btn = $(this);
					btn.disable();
					self.grid.parents(".ui-jqgrid").block({
						message: null,
						overlayCSS: {
							opacity: 0.33,
							cursor: 'auto'
						}
					});

					self.hideColumn("editactions");
				    self.hideColumn("selected");
					var rowIds = self.grid.jqGrid("getDataIDs");

					var editOptions = self.getEditRowOptions();
					editOptions.keys = false;
					editOptions.aftersavefunc = null;
					editOptions.editMode = "editAll";

					Enumerable.From(rowIds).ForEach(function (id) {
						var row = self.getGridRow(id);

						if (row.inEditMode()) {
							row.commitChangesLocal();
						}

						self.editRow(id, editOptions);
					});
					self.disableModals();
					self.resizeHeight();
					setTimeout(function () {
						self.grid.parents(".ui-jqgrid").unblock();
						btn.enable();
					}, (window.navigator.userAgent.contains('MSIE')) ? 1500 : 200);

				});
			}
		},
		registerCancelAllButton: function () {
			/// <summary>
			/// Registers the on click event for a cancel all button.
			/// </summary>
			/// <param name="registration"></param>
			var self = this;

			if (self.editAllButton !== "") {
				$(self.cancelAllButton).on("click", function (e, ui) {

					var btn = $(this);
					btn.disable();
					self.grid.parents(".ui-jqgrid").block({
						message: null,
						overlayCSS: {
							opacity: 0.33,
							cursor: 'auto'
						}
					});
					var rowIds = self.grid.jqGrid('getDataIDs');

					Enumerable.From(rowIds).ForEach(function (id) {
						var row = self.getGridRow(id);
						row.restore();
						self.assureModalEditablity(row);
					});

					
					self.resizeHeight();
					self.registerEditRowEvents();
					self.showColumn("editactions");
				    self.showColumn("selected");
					setTimeout(function () {
						self.grid.parents(".ui-jqgrid").unblock();
						btn.enable();
					}, (window.navigator.userAgent.contains('MSIE')) ? 1500 : 200);


				});
			}
		},
		clearRowSelections: function () {
			$(this.multiSelectCheckBoxSelector + ":checked")
				.each(function (index, elem) {
					$(elem).trigger("click");
				});
		},
		showColumn: function (colName) {
			/// <summary>
			/// Shows the specified column
			/// </summary>
			/// <param name="colName" type="String">Name of the column</param>
			var self = this;
			self.grid.jqGrid('showCol', colName);
			self.resizeGrid();
		},
		hideColumn: function (colName) {
			/// <summary>
			/// Hides the specified column
			/// </summary>
			/// <param name="colName" type="String">Name of the column</param>
			var self = this;
			self.grid.jqGrid('hideCol', colName);
		},
		registerSaveAllButton: function () {
			/// <summary>
			/// 
			/// </summary>
			/// <param name="registration"></param>
			var self = this;

			if (self.saveAllButton !== "") {
				$(self.saveAllButton).on("click", function (e, ui) {
				    var editOptions = self.getEditRowOptions();
				    editOptions.keys = false;
				    editOptions.aftersavefunc = null;

                    var btn = $(this);
					btn.disable();
					self.grid.parents(".ui-jqgrid").block({
						message: null,
						overlayCSS: {
							opacity: 0.33,
							cursor: 'auto'
						}
					});

				    var rowsToProcess = [];
					var batchRows = [];
					var rowIds = Enumerable.From(self.grid.jqGrid('getDataIDs'));
					

					rowIds.ForEach(function (id) {
						var row = self.getGridRow(id);
						if (row.isDirty()) {
						    rowsToProcess.push(row);
						    row.commitChangesLocal();
						} else {
							row.restore();
						}
					});

				    try {
				        if (rowsToProcess && rowsToProcess.length > 0) {
				            self.processBeforeSaveRules(rowsToProcess).done(function (data) {
				                var rows = data.rows;
				                var result = data.result;

				                if (result) {
				                    Enumerable.From(rows).ForEach(function (row) {
				                        batchRows.push(self.resolveRowData(row));
				                    });

				                    if (batchRows.length > 0) {
				                        self.submitBatch(batchRows);
				                    }
				                    else {
				                        self.showColumn("editactions");
				                        self.showColumn("selected");
				                        self.registerEditRowEvents();
				                    }
				                }
				                else {
				                    Enumerable.From(rowsToProcess).ForEach(function (row) {
				                        self.editRow(row.Id(), editOptions);
				                    });
				                }
				            });
				        }
				    }
				    catch (ex) {
				        throw ex;
				    }
				    finally {
				        self.grid.parents(".ui-jqgrid").unblock();
				        btn.enable();
				    }
				});
			}
		},
		resolveRowData: function (row) {

            if(row instanceof GridRow) {
                return row.rowData();
            }

            if (row instanceof Object) {
               return row;
            }

            if (String.isString(row)) {
                return this.getGridRow(row).rowData();
            }

        },
		submitMultiBatchDelete: function (batchRows) {
			var self = this;

			self.proxy.send({ request: { Results: batchRows } },
			self.deleteUrl,
			function (success, dataref, errorDetails) {

				if (success && dataref.success) {

					Enumerable.From(dataref.rows).ForEach(function (x) {
						var row = self.getGridRow(x.Id);
						row.save(x);
						row.disableIcon(".error");
						self.registerEditRowEvents(row.Id());
					});
					var deletedRowIds = (dataref.userdata && dataref.userdata.DeletedIds)
						? dataref.userdata.DeletedIds : [];

					Enumerable.From(deletedRowIds).ForEach(function (id) {
						var row = self.getGridRow(id);
						row.unselect();
						row.deleteRow();
						delete self.gridRowsDict[id];
					});

					self.assureModalEditablity(dataref.rows);
					self.clearRowSelections();
				}
				else  {

					Enumerable.From(batchRows).ForEach(function (rowdata) {
						var row = self.getGridRow(rowdata.Id);
						if (row.isSelected()) {
							// Unselect and reselect reconstitutes deferred promise callbacks
							row.unselect();
							row.select();
						}
					});

					if (dataref) {
					    var msgContent = self.buildMultiRowActionValidationMessage(dataref.validationErrors);
					    UL.ShowAlert("Update Failed", msgContent);
					}
				}
			});
		},
		submitMultiBatch: function (batchRows) {
			var self = this;

			self.proxy.send({ request: { Results: batchRows } },
			self.saveUrl,
			function (success, dataref, errorDetails) {

				if (success && dataref.success) {

					Enumerable.From(dataref.rows).ForEach(function (x) {
						var row = self.getGridRow(x.Id);
						row.save(x);
						row.disableIcon(".error");
						self.registerEditRowEvents(row.Id());
					});

					self.assureModalEditablity(dataref.rows);
					
					self.clearRowSelections();
				}
				else {

					Enumerable.From(batchRows).ForEach(function (rowdata) {
						var row = self.getGridRow(rowdata.Id);
						if (row.isSelected()) {
							row.unselect();
							row.select();
						}
						
					});

                    if (dataref) {
                        var msgContent = self.buildMultiRowActionValidationMessage(dataref.validationErrors);
                        UL.ShowAlert("Update Failed", msgContent);
                    }
				}
			});
		},
		buildMultiRowActionValidationMessage: function (validationErrors) {

			var content = "";

			if (validationErrors && validationErrors.length > 0) {

				var self = this;
				var validationErrorsByRow = Enumerable.From(validationErrors)
					.GroupBy(function (x) { return x.TargetEntityId; });
				var summaryBuilder = MvcJqGrid.GetRenderBehavior(MvcJqGrid.RenderBehavior.ValidationTooltip);



				validationErrorsByRow.ForEach(function (x) {
					var row = self.getGridRow(x.Key());
					content += summaryBuilder(x, "", row.rowData());

				});
			}
			return "<div>" + content + "</div>";
		},
		submitBatch: function (batchRows) {
			var self = this;
			var editOptions = self.getEditRowOptions();
			editOptions.keys = false;
			editOptions.aftersavefunc = null;
			self.hideColumn('editactions');
			self.hideColumn("selected");
			self.proxy.send({ request: { Results: batchRows } },
			self.saveUrl,
			function (success, dataref, errorDetails) {

				if (success && dataref.success) {

					Enumerable.From(dataref.rows).ForEach(function (x) {
						var row = self.getGridRow(x.Id);
						row.save(x);
						row.disableIcon(".error");

					});

					self.registerEditRowEvents();
					self.assureModalEditablity(dataref.rows);
					self.showColumn("editactions");
					self.showColumn("selected");
				}
				else {
					//put back all rows into edit mode
					Enumerable.From(batchRows).ForEach(function (row) {
						self.editRow(row.Id, editOptions);
					});

                    if (dataref) {
                        self.applyValidationErrors(dataref.validationErrors);
                    }
				}
			});

		},
		mergeDirtyObjects: function (orignal, userChanged, serverChanged) {
			return UL.Utility.mergeObjects(orignal, userChanged, serverChanged,
				["*/RecordVersion"]);
		},
		setRowData: function (rowid, data) {
			this.grid.jqGrid('setRowData', rowid, data);
			this.registerEditRowEvents(rowid);
		},
		updateRowsAfterSave: function (data) {
			var self = this;
			Enumerable.From(data).ForEach(function (x) {
				self.setRowData(x.Id, x);
			});
		},
		getSearchCriteria: function () {
			var sortInfo = this.state.state.sortInfo;
			if (sortInfo) {
				this.criteria.SortField = sortInfo.field;
				this.criteria.SortOrder = sortInfo.order;
				this.criteria.Sorts = [{
					FieldName: sortInfo.field,
					Order: sortInfo.order
				}];
			}

			return this.criteria;
		},
		initHeaderTooltips: function () {
			var i;
			var headers = $.find("th.ui-th-column");
			for (i = 0; i < this.columnModels.length; i++) {
				$(headers[i]).attr("title", this.columnModels[i].headerToolTip);
			}
		},
		createActionColumn: function (render) {
			if (this.columnModels) {
				this.columnModels.insert(0, {
					sortable: false,
					align: 'center',
					name: 'actions',
					width: 35,
					fixed: true,
					resizable: false,
					title: "",
					classes: "embed-action-dropdown",
					//this will need to be dynamic and worked
					//into the mvc helper model.                  
					render: render
				});

				this.columnNames.insert(0, "");
			}
		},
		createEditActionsColumn: function () {
			if (this.columnModels) {
				this.columnModels.insert(0, {
					sortable: false,
					align: 'center',
					name: 'editactions',
					width: 45,
					fixed: true,
					resizable: false,
					title: false,
					classes: "embed-action-dropdown",
					render: MvcJqGrid.RenderBehavior.EditActions
				});

				this.columnNames.insert(0, "");
			}
		},
		createValidationSummaryColumn: function () {
			if (this.columnModels) {
				this.columnModels.insert(0, {
					name: 'vsummary',
					sortable: false,
					align: 'center',
					width: 30,
					fixed: true,
					resizable: false,
					title: false,
					classes: "embed-action-dropdown validation-summary-column",
					render: MvcJqGrid.RenderBehavior.ValidationSummary
				});

				this.columnNames.insert(0, "");
			}
		},
		onGridSorted: function (handler) {
			/// <summary>
			/// Registers the specified hadnler for the Grid Column Sorted event
			/// </summary>
			/// <param name="handler" Type="Function(event, args)">The event handler function.</param>

			this.grid.on("sorted.mvcjqGrid", handler);
		},
		getGridRow: function (rowid) {
			/// <summary>
			/// 
			/// </summary>
			/// <param name="rowid" type="String"></param>
			/// <returns type="GridRow"></returns>

			if (!this.gridRowsDict[rowid]) {
				var gRow = new GridRow(this.grid, rowid);
				this.gridRowsDict[rowid] = gRow;
				return gRow;
			}
			return this.gridRowsDict[rowid];
		},
		applyValidationErrors: function (validtionErrors) {
			/// <summary>
			/// 
			/// </summary>
			/// <param name="validtionErrors" type="Array"></param>

			if (validtionErrors && validtionErrors.length > 0) {
				var self = this;
				var validationErrorsByRow = Enumerable.From(validtionErrors)
					.GroupBy(function (x) { return x.TargetEntityId; });

				validationErrorsByRow.ForEach(function (x) {
					var row = self.getGridRow(x.Key());
					row.showColumnErrors(x.source);

					if (self.includeValidationSummary) {
						row.applyValidationSummary(x.source);
					}

				});
			}
		},
		configureColumns: function () {
			/// <summary>
			/// Configures the column models with functions for routing to 
			/// the appropriate format strategy.
			/// </summary>

			Enumerable.From(this.columnModels).ForEach(function (col) {

				col.formatter = MvcJqGrid.FormatRouter;
				col.unformat = MvcJqGrid.UnFormatRouter;

				if (col.template) {
					var template = MvcJqGrid.GetColumnTemplate(col.template);
					var tempClasses = col.classes;
					$.extend(col, template);
					//merge class property, don't overwrite.
					if (tempClasses) {
						col.classes = tempClasses + "," + col.classes;
					}
				}

				if (col.editable) {
					var editorColumnProperties = MvcJqGrid.GetEditorConfiguration(col.editorType);
					$.extend(col, editorColumnProperties);
				}
			});
		}
	};

	//#endregion


	//#region JQuery Widget Hook 

	$.fn.mvcJqGrid = function (options, optionValue) {
		/// <summary>
		/// 
		/// </summary>
		/// <param name="options"></param>
		/// <param name="optionValue"></param>

		var objDataKey = UL.MvcJqGrid.DataKey;
		if (typeof options === 'string') {

			var gridRegObj = $(this[0]).data(objDataKey);
			if (gridRegObj !== null) {
				try {
					gridRegObj[options](optionValue);
				}
				catch (e) {
					throw options.toString() + " was an invalid behavior.";
				}
			}
		}
		else {

			$(this).each(function (index, elem) {

				var jqElem = $(elem);
				var gr = new GridRegistration(new UL.Proxy());
				gr = $.extend(gr, jqElem.data());
				gr.element = jqElem;
				gr.selector = jqElem.context.id;
				MvcJqGrid.grids[gr.selector] = gr;
				jqElem.data(objDataKey, gr);
				gr.init();

			});

			if ($(this).length > 0) {
				MvcJqGrid.EnableGlobalLossPrevention();
			}
		}

		return $(this);

	};


	$(document).ready(function () {
		$(".ui-jqgrid").on("mouseover", ".dropdown.hover-menu", function () {
			var self = $(this);
			var link = self.find("a");
			var dropdownTop = (link.offset().top + link.outerHeight()) - $(window).scrollTop();
			var dropdown = self.find(".dropdown-menu");
			dropdown.css('position', "fixed");
			dropdown.css('top', dropdownTop + "px");
			dropdown.css('left', link.offset().left + "px");
		});
	});
 
}(jQuery));



