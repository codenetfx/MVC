/// <reference path="../_references.js" />
/// <reference path="../_ULReferences.js" />
/// <reference path="../../Lib/jquery/plugins/linq-vsdoc.js" />
/// <reference path="../../Lib/jquery/plugins/linq.js" />

(function ($) {
    'use strict';
    if (!window.UL) {
        window.UL = {};
    }

    $.fn.initProjectToggleLinks = function () {
        /// <summary>
        /// Initilizes the project links
        /// Note: currently not in use, reason currently unknown mvb
        /// </summary>

        var jqResults = $(this);
        jqResults.on("click", function (e, ui) {

            e.preventDefault();

            var anchor = $(this);
            var url = anchor.prop("href");
            var visible = (anchor.text() === 'hide');
            var text = visible ? 'unhide' : 'hide';
            var desc = visible ? 'Hidden from Customer ' : 'Visible to Customer ';
            var proxyOptions = new UL.ProxyOptions();

            var proxy = new UL.Proxy();
            proxy.send(null, url,
				function (successful, data, error) {

				    if (successful && data.Successful) {
				        anchor.text(text);
				        anchor.parent().prev().text(desc);
				    }
				    else {
				        window.alert("unable to hide project because of some error");
				    }
				},
				proxyOptions);

        });
    };

    UL.NavigateToLogin = function () {
        window.location.reload();
    };

    UL.GetExceptionModalHtml = function (title, body, addLoginRedirect) {
        /// <summary>
        /// Gets a model for displaying an exception.
        /// </summary>
        /// <param name="title" type="String">Modal Dialog title</param>
        /// <param name="body" type="String">the html formatted content.</param>
        /// <param name="addLoginRedirect">adds a redirect button if true, otherwise no button is displayed.</param>
        /// <returns type="String">the html string representation of the exception in a model. </returns>

        var loginbtn = '<button class="btn btn-info pull-right login-btn" style="margin-left:10px" aria-hidden="true">Login</button>';

        return '<div class="modal-header"><button type="button" class="close" data-dismiss="modal" aria-hidden="true">×</button><h3>'
        + title + '</h3></div><div class="modal-body" id="modal-form">'
        + body + '</div><div class="modal-footer error-footer">'
        + (addLoginRedirect ? loginbtn : ' ')
        + '<button class="btn btn-info pull-right" data-dismiss="modal" aria-hidden="true">Dismiss</button>'
        + '</div>';
    };

    UL.HandleAjaxUnhandledError = function (jqXHR, textStatus, errorThrown) {
        /// <summary>
        /// Show a model displaying using the specified ajax error state information.
        /// </summary>
        /// <param name="jqXHR" type="jqResponse">Jquery request object.</param>
        /// <param name="textStatus" type="String">The status</param>
        /// <param name="errorThrown" string"Object">the exception object.</param>
        /// <returns type="jqResult">Bootstrap Model jqqery Result Ref</returns>

        var content = errorThrown || "An unexpected error has occurred.";
        try {

            var responseContent = "";
            var errorPage = "";
            var loweredResponse = jqXHR.responseText.toLowerCase();

            var isFullPageResponse = (loweredResponse && loweredResponse.indexOf("</html>") >= 0);
            if (isFullPageResponse) {
                var lowerBound = loweredResponse.indexOf("<body>");
                var upperBound = loweredResponse.indexOf("</body>");
                responseContent = $(jqXHR.responseText.substr(lowerBound, upperBound - lowerBound));
                errorPage = responseContent.find(".body-content").attr("style", "min-width:auto; min-height:auto");
            }
            else {
                responseContent = $(jqXHR.responseText.trim());
                errorPage = responseContent.find("div.error-page");
            }

            if (errorPage.length > 0) {
                var tempBody = errorPage.htmlAll();
                content = UL.GetExceptionModalHtml("An Error Has Occurred!", tempBody);
            }
            else {
                content = responseContent.htmlAll();
            }
        } catch (e) {
            try {

                content = UL.GetExceptionModalHtml("An Error Has Occurred!",
                    "The cause of the error was unable to be reported to the browser. Please see a system administrator if the issue persists.");

            }
            catch (ignore) { }
        }
        return $('<div class="modal hide fade" >' + content + '</div>').modal();
    };


    UL.HandleAjaxUnhandledErrorContentAll = function (content) {
        /// <summary>
        /// Show a model displaying using the specified ajax error state information.
        /// </summary>      
        /// <param name="content">the conent to be displayed in the model.</param>
        /// <returns type="jqResult">Bootstrap Model jqqery Result Ref</returns>

        return $('<div class="modal hide fade">' + content + '</div>').modal();
    };

    UL.HandleAjaxError = function (jsonResponse) {
        /// <summary>
        /// Shows handled errors in a modal.
        /// </summary>
        /// <param name="jsonResponse">The json resposne</param>
        /// <returns type="jqResult">Bootstrap Model jqqery Result Ref</returns>

        var Is400Error = (jsonResponse.ErrorCode >= 400 && jsonResponse.ErrorCode < 500);
        var title = Is400Error ? jsonResponse.ErrorCode + " Error" : "An Error Has Occurred!";

        var content = UL.GetExceptionModalHtml(title, jsonResponse.Message, Is400Error);

        var myModal = $('<div class="modal hide fade">' + content + '</div>').modal();

        $(".login-btn").click(function (e) {
            e.preventDefault();
            UL.NavigateToLogin();
            return false;
        });

        return myModal;
    };

    UL.ShowAlert = function (title, message, callback) {
        /// <summary>
        /// Display an alert modal to the user.
        /// </summary>
        /// <param name="title" type="String">The Model Title</param>
        /// <param name="message" type="String">To message content</param>
        /// <param name="callback" type="Function" optional="">To message content</param>
        /// <returns type="jQuery">Bootstrap Model jqqery Result Ref</returns>

        var content = '<div class="modal-header"><button type="button" class="close" data-dismiss="modal" aria-hidden="true">×</button><h3>'
        + title + '</h3></div><div class="modal-body" id="modal-form">'
        + message + '</div><div class="modal-footer error-footer">'
        + '<button class="btn btn-info pull-right" data-dismiss="modal" aria-hidden="true">Dismiss</button>'
        + '</div>';
        var modal = $('<div class="modal hide fade">' + content + '</div>');

        if(typeof callback === "function"){
            modal.find(".btn").on("mousedown", callback);
        }
     
        return modal.modal();
    };


    UL.ShowConfirm = function (title, message, callback) {
        /// <summary>
        /// Display an alert modal to the user.
        /// </summary>
        /// <param name="title" type="String">The Model Title</param>
        /// <param name="message" type="String">To message content</param>
        /// <returns type="jqResultModalRef">Bootstrap Model jqqery Result Ref</returns>

        var content = '<div class="modal-header"><button type="button" class="close" data-dismiss="modal" aria-hidden="true">×</button><h3>'
        + title + '</h3></div><div class="modal-body" id="modal-form">'
        + message + '</div><div class="modal-footer error-footer" style="text-align:center;">'
		+ ' <button id="btnYes" class="btn btn-primary">Yes</button>'
        + '<button class="btn btn-info" data-dismiss="modal" aria-hidden="true">No</button>'
        + '</div>';
        var modal = $('<div class="modal hide fade">' + content + '</div>').modal();

        modal.find('button').on("click", function (e) {
            if (callback && typeof callback === "function") {
                var btnText = $(this).text();

                var ui = {
                    result: btnText === "x" ? 'No' : btnText,
                    modal: modal
                };
                callback(ui);
            }
        });
        return modal;
    };

    UL.ModalOptions = function () {
        this.title = "Title";
        this.message = null;
        this.cancelButtonText = "Cancel";
        this.submitButtonText = "Save";
        this.callback = null;

    };

    UL.ShowModal = function (initOptions) {
        /// <summary>
        /// Display an alert modal to the user.
        /// </summary>
        /// <param name="title" type="String">The Model Title</param>
        /// <param name="message" type="String">To message content</param>
        /// <returns type="jqResultModalRef">Bootstrap Model jqqery Result Ref</returns>

        var opt = (initOptions && (initOptions instanceof UL.ModalOptions))
            ? initOptions : new UL.ModalOptions();

        var content = '<div class="modal-header"><button type="button" class="close" data-dismiss="modal" aria-hidden="true">×</button><h3>'
        + opt.title + '</h3></div><div class="modal-body" id="modal-form">'
        + opt.message + '</div><div class="modal-footer error-footer" style="text-align:center;">'
		+ ' <button id="btnYes" class="btn btn-primary pull-right">' + opt.submitButtonText + '</button>'
        + '<button class="btn btn-info pull-left" data-dismiss="modal" aria-hidden="true">' + opt.cancelButtonText + '</button>'
        + '</div>';
        var modal = $('<div class="modal hide fade">' + content + '</div>').modal();

        modal.find('button').on("click", function (e) {
            if (opt.callback && typeof opt.callback === "function") {
                var btnText = $(this).text();
                var ui = {
                    result: btnText === "x" ? 'No' : btnText,
                    modal: modal
                };
                opt.callback(ui);
            }
        });
        return modal;
    };


}(jQuery));




