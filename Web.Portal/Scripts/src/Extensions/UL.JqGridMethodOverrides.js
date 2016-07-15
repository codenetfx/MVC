/// <reference path="../_references.js" />

//*** IMPORTANT
//*** Allows the tree expand/colapse field to have a custom renderer/Formatter to work property when expando field is also editable.
//*** orignal code did not call the unformat so editing would have been impossible without the change.
//*** Due to this code only getting bare minimum changes to support the fix. 
//*** The source code style is not modified to pass jslint standards, therefore it has been excluded from the jslint checks.

(function () {
    'use strict';

    $.jgrid.extend({
        getRowData: function (rowid) {
            /// <summary>
            /// This method has been  overriden because the
            /// </summary>
            /// <param name="rowid"></param>

            var res = {}, resall, getall = false, len, j = 0;
            this.each(function () {
                var $t = this, nm, ind;
                if (rowid === undefined) {
                    getall = true;
                    resall = [];
                    len = $t.rows.length;
                } else {
                    ind = $($t).jqGrid('getGridRowById', rowid);
                    if (!ind) { return res; }
                    len = 2;
                }
                while (j < len) {
                    if (getall) { ind = $t.rows[j]; }
                    if ($(ind).hasClass('jqgrow')) {
                        $('td[role="gridcell"]', ind).each(function (i) {
                            nm = $t.p.colModel[i].name;
                            if (nm !== 'cb' && nm !== 'subgrid' && nm !== 'rn') {
                                if ($t.p.treeGrid === true && nm === $t.p.ExpandColumn) {

                                    try {
                                        res[nm] = $.unformat.call($t, $("span:first", this), { rowId: ind.id, colModel: $t.p.colModel[i] }, i);
                                    } catch (e) {
                                        res[nm] = $.jgrid.htmlDecode($("span:first", this).html());
                                    }
                                } else {
                                    try {
                                        res[nm] = $.unformat.call($t, this, { rowId: ind.id, colModel: $t.p.colModel[i] }, i);
                                    } catch (e) {
                                        res[nm] = $.jgrid.htmlDecode($(this).html());
                                    }
                                }
                            }
                        });
                        if (getall) { resall.push(res); res = {}; }
                    }
                    j++;
                }
            });

            return resall || res;
        }
        ,
        editRow: function (rowid, keys, oneditfunc, successfunc, url, extraparam, aftersavefunc, errorfunc, afterrestorefunc) {
            // Compatible mode old versions
            var o = {}, args = $.makeArray(arguments).slice(1);

            if ($.type(args[0]) === "object") {
                o = args[0];
            } else {
                if (keys !== undefined) { o.keys = keys; }
                if ($.isFunction(oneditfunc)) { o.oneditfunc = oneditfunc; }
                if ($.isFunction(successfunc)) { o.successfunc = successfunc; }
                if (url !== undefined) { o.url = url; }
                if (extraparam !== undefined) { o.extraparam = extraparam; }
                if ($.isFunction(aftersavefunc)) { o.aftersavefunc = aftersavefunc; }
                if ($.isFunction(errorfunc)) { o.errorfunc = errorfunc; }
                if ($.isFunction(afterrestorefunc)) { o.afterrestorefunc = afterrestorefunc; }
                // last two not as param, but as object (sorry)
                //if (restoreAfterError !== undefined) { o.restoreAfterError = restoreAfterError; }
                //if (mtype !== undefined) { o.mtype = mtype || "POST"; }			
            }
            o = $.extend(true, {
                keys: false,
                oneditfunc: null,
                successfunc: null,
                url: null,
                extraparam: {},
                aftersavefunc: null,
                errorfunc: null,
                afterrestorefunc: null,
                restoreAfterError: true,
                mtype: "POST"
            }, $.jgrid.inlineEdit, o);

            // End compatible
            return this.each(function () {

                var $t = this, nm, tmp, editable, cnt = 0, focus = null, svr = {}, ind, cm, bfer;
                if (!$t.grid) { return; }
                ind = $($t).jqGrid("getInd", rowid, true);
                if (ind === false) { return; }
                bfer = $.isFunction(o.beforeEditRow) ? o.beforeEditRow.call($t, o, rowid) : undefined;
                if (bfer === undefined) {
                    bfer = true;
                }
                if (!bfer) { return; }
                editable = $(ind).attr("editable") || "0";
                if (editable === "0" && !$(ind).hasClass("not-editable-row")) {
                    cm = $t.p.colModel;
                    $('td[role="gridcell"]', ind).each(function (i) {
                        nm = cm[i].name;
                        var treeg = $t.p.treeGrid === true && nm === $t.p.ExpandColumn;
                        if (treeg) {
                            try {
                                tmp = $.unformat.call($t, $("span:first", this), { rowId: rowid, colModel: cm[i] }, i);
                            } catch (_) {
                                tmp = $("span:first", this).html();
                            }
                        }
                        else {
                            try {
                                tmp = $.unformat.call($t, this, { rowId: rowid, colModel: cm[i] }, i);
                            } catch (_) {
                                tmp = (cm[i].edittype && cm[i].edittype === 'textarea') ? $(this).text() : $(this).html();
                            }
                        }
                        if (nm !== 'cb' && nm !== 'subgrid' && nm !== 'rn') {
                            if ($t.p.autoencode) { tmp = $.jgrid.htmlDecode(tmp); }
                            svr[nm] = tmp;
                            if (cm[i].editable === true) {
                                if (focus === null) { focus = i; }
                                if (treeg) { $("span:first", this).html(""); }
                                else { $(this).html(""); }
                                var opt = $.extend({}, cm[i].editoptions || {}, { id: rowid + "_" + nm, name: nm });
                                if (!cm[i].edittype) { cm[i].edittype = "text"; }
                                if (tmp === "&nbsp;" || tmp === "&#160;" || (tmp.length === 1 && tmp.charCodeAt(0) === 160)) { tmp = ''; }
                                var elc = $.jgrid.createEl.call($t, cm[i].edittype, opt, tmp, true, $.extend({}, $.jgrid.ajaxOptions, $t.p.ajaxSelectOptions || {}));
                                $(elc).addClass("editable");
                                if (treeg) { $("span:first", this).append(elc); }
                                else { $(this).append(elc); }
                                $.jgrid.bindEv.call($t, elc, opt);
                                //Again IE
                                if (cm[i].edittype === "select" && cm[i].editoptions !== undefined && cm[i].editoptions.multiple === true && cm[i].editoptions.dataUrl === undefined && $.jgrid.msie) {
                                    $(elc).width($(elc).width());
                                }
                                cnt++;
                            }
                        }
                    });
                    if (cnt > 0) {
                        svr.id = rowid; $t.p.savedRow.push(svr);
                        $(ind).attr("editable", "1");
                        setTimeout(function () { $("td:eq(" + focus + ") input", ind).focus(); }, 0);
                        if (o.keys === true) {
                            $(ind).bind("keydown", function (e) {
                                if (e.keyCode === 27) {
                                    $($t).jqGrid("restoreRow", rowid, o.afterrestorefunc);
                                    if ($t.p._inlinenav) {
                                        try {
                                            $($t).jqGrid('showAddEditButtons');
                                        } catch (eer1) { }
                                    }
                                    return false;
                                }
                                if (e.keyCode === 13) {
                                    var ta = e.target;
                                    if (ta.tagName === 'TEXTAREA') { return true; }
                                    if ($($t).jqGrid("saveRow", rowid, o)) {
                                        if ($t.p._inlinenav) {
                                            try {
                                                $($t).jqGrid('showAddEditButtons');
                                            } catch (eer2) { }
                                        }
                                    }
                                    return false;
                                }
                            });
                        }
                        $($t).triggerHandler("jqGridInlineEditRow", [rowid, o]);
                        if ($.isFunction(o.oneditfunc)) { o.oneditfunc.call($t, rowid); }
                    }
                }
            });
        }
    });
}(jQuery));





