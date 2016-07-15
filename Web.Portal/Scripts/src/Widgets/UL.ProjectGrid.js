/// <reference path="UL.MvcJqGrid.js" />

(function () {
    'use strict';

    //namespace alias for MvcJqGrid Static api.
    var MvcJqGrid = UL.MvcJqGrid;
    
    UL.ProjectGrid = {
        ProjectStatusIndicator: "UL.ProjectGrid.ProjectStatusIndicator",
        EndDateStatusIndicator: "UL.ProjectGrid.EndDateStatusIndicator",
        OrderLinesIndicator: "UL.ProjectGrid.OrderLinesIndicator",
        ProjectNameFormatter: "UL.ProjectGrid.ProjectNameFormatter",
        PastDueIndicator: "UL.ProjectGrid.PastDueIndicator",
        ActionMenu: "UL.ProjectGrid.ActionMenu",
        statusRender: function (text, statusColorClass, textColorClass) {
            /// <summary>
            /// 
            /// </summary>
            /// <param name="text">The cell value</param>
            /// <param name="statusColorClass" type="String">The status color css class to be applied</param>
            /// <param name="textColorClass" type="String">the text color css class to be applied.</param>
            /// <returns type="String">The formated cell value</returns>

            if (text === "") {
                return "";
            }

            if (!textColorClass) {
                textColorClass = '';
            }

            var div = $(document.createElement('div'))
                .addClass("status-cell")
                .append($(document.createElement('div'))
                    .append($(document.createElement('div'))
                        .addClass("status-marker")
                        .addClass(statusColorClass)
                        .text("")
                    )
                    .append($(document.createElement('div'))
                        .addClass("status-marker-value")
                        .addClass(textColorClass)
                        .text(text)
                    ));

            return div.htmlAll();
        }
    };
    

    //#region Register Project Grid Custom Render Behaviors

    MvcJqGrid.RegisterRenderBehavior(UL.ProjectGrid.ProjectNameFormatter, function (cellvalue, options, rowObject) {
        if (rowObject.RowType !== 1) {
            if (!cellvalue) {
                return '';
            }
            if (!options.url) {
                return cellvalue;
            }
            var a = $(document.createElement('a'));
            a.prop('href', String.formatTokens(options.url, rowObject))
       .text(cellvalue);
            return a.htmlAll();
        }

        var b = $(document.createElement('a'));
        b.prop('href', String.formatTokens("/Request/IncomingOrderDetails/{Id}", rowObject))
            .text("Unassigned");
        return b.htmlAll();
    });

    MvcJqGrid.RegisterRenderBehavior(UL.ProjectGrid.PastDueIndicator, function (cellvalue, options, rowObject) {
        var numTasks = parseInt(cellvalue, 10);
        if (numTasks === 0) {
            return "";
        }

        var textColorClass = "red-text",
            statusColorClass = 'red-back';

        var marker = $(document.createElement('div'))
            .append($(document.createElement('div'))
                .addClass("status-marker")
                .addClass(statusColorClass)
                .text(" ")
            );
        var div = $(document.createElement('div'))
                            .addClass("status-cell")
                            .append(marker);
        if (!options.url) {
            marker.append($(document.createElement('div'))
                .addClass("status-marker-value")
                .addClass(textColorClass)
                .text(numTasks)
            );
        } else {
            var a = $(document.createElement('a'));
            a.prop('href', String.formatTokens(options.url, rowObject))
                .text(cellvalue)
                .addClass("status-marker-value")
                .addClass(textColorClass);
            marker.append(a);
        }
        return div.htmlAll();
    });

    MvcJqGrid.RegisterRenderBehavior(UL.ProjectGrid.OrderLinesIndicator, function (cellvalue, options, rowObject) {

        if (rowObject.RowType !== 1) {
            return cellvalue;
        }
        var
            statusColorClass = 'yellow';

        var marker = $(document.createElement('div'))
            .append($(document.createElement('div'))
                .addClass("status-marker")
                .addClass(statusColorClass)
                .text(" ")
            );
        var div = $(document.createElement('div'))
                            .addClass("status-cell")
                            .append(marker);
        if (!options.url) {
            marker.append($(document.createElement('div'))
                .addClass("status-marker-value")
                .text(cellvalue)
            );
        } else {
            var a = $(document.createElement('a'));
            a.prop('href', String.formatTokens(options.url, rowObject))
                .text(cellvalue)
                .addClass("status-marker-value");
            marker.append(a);
        }
        return div.htmlAll();
    });

    MvcJqGrid.RegisterRenderBehavior(UL.ProjectGrid.EndDateStatusIndicator, function (cellvalue, options, rowObject) {

        if (rowObject.RowType !== 0 || cellvalue === '1-01-01' || cellvalue === '0-12-31' || cellvalue === '0001-01-01') {
            return "";
        }
        var formatter = UL.ProjectGrid.statusRender;

        var textColor = '',
            statusColor = '';

        if (rowObject.EndDateIsCritical) {
            textColor = "red-text";
            statusColor = 'red-back';
        }

        return formatter(cellvalue, statusColor, textColor);
    });

    MvcJqGrid.RegisterRenderBehavior(UL.ProjectGrid.ProjectStatusIndicator, function (cellvalue, options, rowObject) {

        var formatter = UL.ProjectGrid.statusRender;
        var color = '';
        switch (cellvalue) {
            case 'In Progress':
                color = '';
                break;
            case 'On Hold':
                color = 'yellow';
                break;
            case 'Completed':
                color = 'green';
                break;
        }

        return formatter(cellvalue, color);
    });

    MvcJqGrid.RegisterRenderBehavior(UL.ProjectGrid.ActionMenu, function (cellValue, options, rowObject) {

        if (rowObject.RowType !== 0) {
            return "";
        }

        var div = $(document.createElement('div'))
            .addClass('result-actions')
            .css("margin-top", "2px")
            .append($(document.createElement('div'))
                .addClass('dropdown')
                .append($(document.createElement('a'))
                    .addClass('dropdown-toggle caret')
                    .attr('data-toggle', "dropdown")
                    .prop('href', '#')
                    .text(" ")
                )
                .append($(document.createElement('ul'))
                    .addClass("dropdown-menu")
                    .attr('role', "menu")
                    .append($(document.createElement('li'))
                        .append($(document.createElement('a'))
                            .prop("href", '/project/?id=' + rowObject.Id)
                            .text("Project Details")
                        )

                    )
                    .append($(document.createElement('li'))
                        .append($(document.createElement('a'))
                            .prop("href", '/project/' + rowObject.Id + '/Task')
                            .text("Task Details")
                        )
                    )
                )
            );

        return div.htmlAll();
    });

    //#endregion

}());
