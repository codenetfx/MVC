/// <reference path="_references.js" />

if (!window.UL) {     
    window.UL = {};    
}

UL._siteRoot = "/"//updated with correct path from master page
$.fn.modal.defaults.backdrop = "static"; // suppress the function that dismisses the dialog by clicking outside of it.
$.fn.modal.defaults.attentionAnimation = null;
$.fn.modal.defaults.spinner = UL.loadAnimationLarge[0]; //html for the progress bar has been moved to global as a js cleanup migration step. MVB
$.fn.modalmanager.defaults.spinner = UL.loadAnimationLarge[0];



$(document).ready(function () {

    var headerConfig = {
        timing: 550,
        timing_fast: 250,
        dropdown_speed: 200
    };

    function initHeader() {
        // open and close header
        $("#myHomeButton, #header .close_down").click(function (e) {
            e.preventDefault();
            $('#header .header_flyup').slideToggle(headerConfig.timing, function () {
                $("#myHomeButton").toggleClass("up");
            });
        });

        // focus and blue global search box
        $('#header_search input').focus(function (e) {
            $('.search-wrap').addClass('focused');
            $('.search-left').addClass('focused');
            $('.submit').addClass('focused');
        });
        $('#header_search input').blur(function () {
            $('.search-wrap').removeClass('focused');
            $('.search-left').removeClass('focused');
            $('.submit').removeClass('focused');
        });
    }

    function initHeaderRefiners() {
        var header = $('.search-header legend');
        var refiner = header.find('.refine-list');
        var actions = header.find('.actions-list');
        var refinerList = refiner.find('li');

        var headerWidth = header.width();
        var titleWidth = header.find('h2').outerWidth(true);
        var actionWidth = actions.outerWidth(true) || 0;
        var availableWidth = headerWidth - titleWidth - actionWidth;
        var refinerWidth = refiner.outerWidth(true);

        var moreList = $('<li></li>').attr('id', 'more-list').html('<div class="dropdown pull-right"><a href="#" data-toggle="dropdown" title="more refiners">more...</a><ul class="dropdown-menu search-menu" role="menu"></ul></div>');

        if (refinerWidth < availableWidth) {
            refiner.css('visibility', 'visible');
        } else {
            //add the "more..." item and remove its width from available
            refiner.append(moreList);
            availableWidth -= moreList.outerWidth(true);

            refinerList.each(function () {
                availableWidth -= $(this).outerWidth(true);
                if (availableWidth <= 0) {
                    moreList.find('ul').append($(this));
                }
            });
            refiner.css('visibility', 'visible');
        }
    }

    function initLeftNavRefiners() {
        $("a.refine").click(function (e) {
            e.preventDefault();
            $('ul.hidden').hide();
            if ($(this).hasClass('open') == false) {
                $(this).addClass('open');
                $(this).parent().find('ul').fadeIn('fast').delay(3500).fadeOut('fast', function () { $(this).prev("a").removeClass('open'); });
            }
        });

        $('li.menuLevel0 a.accordion-toggle:first').click(function (e) {
            e.preventDefault();
            $(this).parent('li').find('div.accordion-body:first').toggle();
            $(this).toggleClass('toggle-plus');
            return false;
        });
    }


    // execute
    initHeader();
    initHeaderRefiners();
    initLeftNavRefiners();
});

//
// static helper functions
//
(function ($) {
    UL.hookCancelButtons = function () {
        $('input[value=Cancel]').click(function (event) {
            event.preventDefault();
            window.location = ($(this).data("redirect") || window.location);
        });
    };

    UL.ValidateModal = function validator() {

        var valid = $(this).validate().form();
        if (valid) {
            var modal = $(this).closest(".modal");
            //hide/clean up any existing messages
            try { modal.modal('removeLoading'); } catch (ex) { }
            modal.modal('loading');
        }

        return valid;
    };

    UL.CreateModalChrome = function (element) {
        var opts = { "class": "modal hide fade ", tabindex: -1 };
        var data = {};
        if (element) {
            element = $(element);
            opts["class"] += element.data("modalClass");
            data = element.data();
        }

        var $hole = $('#blackHole');//from master page
        var $modal = $("<div />", opts);
        $modal.data(data);
        $hole.empty();
        $hole.append($modal);

        return $modal;
    };

    UL.CreateModal = function (title, body) {
        var html = '\
<div class="modal-header">\
	<button type="button" class="close" data-dismiss="modal" aria-hidden="true">×</button>\
	<h3>' + title + '</h3>\
</div>\
<div class="modal-body" id="modal-form">\
	' + body + '\
</div>\
<div class="modal-footer error-footer">\
	<button class="btn btn-info pull-right" data-dismiss="modal" aria-hidden="true">Dismiss</button>\
</div>';

        return $(html);
    };

    UL.CreateErrorHtml = function (message) { return UL.CreateAlertHtml(message, "alert-error"); };

    UL.CreateWarningHtml = function (message) { return UL.CreateAlertHtml(message, "alert-block"); };

    UL.CreateSuccessHtml = function (message) { return UL.CreateAlertHtml(message, "alert-success"); };

    UL.CreateInfoHtml = function (message) { return UL.CreateAlertHtml(message, "alert-info"); };

    UL.CreateAlertHtml = function (message, className) {
        return '<div class="alert ' + className + '"><button type="button" class="close" data-dismiss="alert">×</button>' + message + '</div>';
    };

    UL.GetQueryStringHash = function () {
        var args = new Object();
        var params = window.location.toString().split('?')[1];

        if (params != undefined && params.length > 0) {
            var queryStringParms = params.split("#")[0].split('&');
            for (var i = 0; i < queryStringParms.length; i++) {
                var hash = queryStringParms[i].split('=');
                args[decodeURIComponent(hash[0])] = decodeURIComponent(hash[1]);
            }
        }

        return args;
    };

    UL.Refresh = function () {
	   window.location = window.location.toString().split("#")[0];
    };
    UL.RefreshNoQueryString = function() {
        window.location = window.location.toString().split("?")[0];
    };

    UL.BaseUrl = function () {
        return window.location.toString().split("?")[0];
    };

    UL.ResetSearch = function (args) {
        delete args["Paging.Page"];
    };

    UL.RefineSearch = function (element) {
        element = $(element);
        var key = element.data("refinerKey");
        var value = element.data("refinerValue");
        var argKey = "Filters." + key;

        var args = UL.GetQueryStringHash();
        delete args["useDefaultSearch"];

        if (value === "")
            delete args[argKey];
        else
            args[argKey] = value;

        UL.ResetSearch(args);

        var url = UL.BaseUrl() + "?" + $.param(args) + window.location.hash;
        window.location = url;

        return false;
    };

    UL.RemoveQuery = function (element) {


        var args = UL.GetQueryStringHash();
        delete args["query"];
        delete args["Query"];
        delete args["useDefaultSearch"];
        UL.ResetSearch(args);

        var url = UL.BaseUrl() + "?" + $.param(args) + window.location.hash;
        window.location = url;

        return false;
    };

    UL.Sort = function (element) {
        element = $(element);
        var args = UL.GetQueryStringHash();

        args["searchCriteria.Sorts[0].FieldName"] = element.data("sortField");
        args["searchCriteria.Sorts[0].Order"] = element.data("sortOrder");
        UL.ResetSearch(args);

        var url = UL.BaseUrl() + "?" + $.param(args) + window.location.hash;
        window.location = url;

        return false;
    };

    // fired when a search result checkbox is clicked
    UL.Result_OnChange = function (element) {
        element = $(element);
        var groupName = element.data("groupName");
        var method = element.is(':checked') ? "POST" : "DELETE";
        var id = element.val();
        return UL.GroupItem(id, groupName, method);
    };

    UL.GroupItem = function (id, groupName, method) {
        var url = UL._siteRoot + "Search/GroupItem";
        var args = {
            id: id,
            group: groupName
        };
        var opts = {
            data: args,
            type: method,
            success: function (response, status, jqXhr) {
                var total = response.TotalCount;
                $(".selection-pane")[total > 0 ? "show" : "hide"]();
                $(".selected-count").html(total);
            }
        };

        $.ajax(url, opts);
        return true;
    };

    UL.FormatBytes = function (size) {
        var fileSize = size / 1024;
        var suffix = 'KB';
        if (fileSize > 1000) {
            fileSize = fileSize / 1024;
            suffix = 'MB';
        }
        var fileSizeParts = fileSize.toString().split('.');
        fileSize = fileSizeParts[0];
        if (fileSizeParts.length > 1) {
            fileSize += '.' + fileSizeParts[1].substr(0, 2);
        }
        fileSize += suffix;

        return fileSize;
    };

    // Gets the version of IE browser. 
    // If you're not using a IE browser, or
    // browser can't be detected,
    // return will be 'undefined'.
    UL.GetIEVersion = (function() {
        var undef,
            v = 3,
            div = document.createElement('div'),
            all = div.getElementsByTagName('i');

        while (
            div.innerHTML = '<!--[if gt IE ' + (++v) + ']><i></i><![endif]-->',
            all[0]
        );

        return v > 4 ? v : undef;
    });

    UL.AddErrorMessage = function(message, container) {
        var valMsg = "<li>" + message + "</li>";
        var errorSummary = container.find('.validation-summary-errors');
        if (errorSummary.length == 1 && errorSummary[0].innerHTML != '' && errorSummary[0].innerHTML != undefined) {
            container.find(".validation-summary-errors ul").empty().append(valMsg);
        } else {
            $('#listError').remove();
            $('<div class="validation-summary-errors"></div>').insertAfter(container.find('.validation-summary-valid'));
            $(".validation-summary-errors").append("<ul id='listError'>" + valMsg + "</ul>");
        }
    }

})(jQuery);


(function ($) {
    $(document).ready(function () {
        if ($.fn.liveTile) {
            $.fn.liveTile.defaults = {
                speed: 200,
                delay: 0,
                onHoverDelay: 200,
                repeatCount: 1
            };
        }
    });
})(jQuery);





/* IE8 File Upload Support */
(function ($) {
    //
    // This is used by product & product family upload.
    // Supports only a single file at a time and has a progress bar.
    // Uses a hidden IFRAME + SignalR
    //
    $.fn.fancyUpload = function (options) {

        var settings = $.extend({
            maxSize: 131072000, /* bytes */
            hoverClass: 'file-hover',
            dropHole: null,
            ajaxUploadEnabled: false
        }, options);

        // file drag hover
        function FileDragHover(e) {
            e.stopPropagation();
            e.preventDefault();

            if (e.type == "dragover")
                $(e.target).addClass(settings.hoverClass);
            else
                $(e.target).removeClass(settings.hoverClass);
        }

        // file selection
        function FileSelectHandler(e) {
            if (ajaxUploadSupported) {
                // cancel event and hover styling
                FileDragHover(e);

                // fetch FileList object, first is from file input, second is from drag/drop
                var files = e.target.files || e.dataTransfer.files;

                // process all File objects
                for (var i = 0, f; f = files[i]; i++) {
                    ParseFile(f);
                    //UploadFile(f);
                }
            }
            else {
                //LegacyUpload(e.target);
            }
        }

        var FormSubmit = function(event) {
            event.stopPropagation();
            event.preventDefault();

            var fileInputData = fileInput.data();
	        
            if (fileInputData && fileInputData.val && !fileInput.val()) {
            	var valMsg = fileInputData.valRequired || "<li>Document is required.</li>";
               var errorSummary = form.find('.validation-summary-errors');
                if (errorSummary.length == 1 && errorSummary[0].innerHTML != '' && errorSummary[0].innerHTML != undefined) {
                    form.find(".validation-summary-errors ul").empty().append(valMsg);
                } else {

                    $('#listError').remove();
                    $('<div class="validation-summary-errors"></div>').insertAfter(form.find('.validation-summary-valid'));
                    $(".validation-summary-errors").append("<ul id='listError'>" + valMsg + "</ul>");
                }
                return false;
            }

            if (form.valid()) {
                $uploadPane.hide();
                $errorPane.hide();
                progress.removeClass("bar-danger").removeClass("bar-success");
                progress.parent().addClass("active");
                $progressPane.show();
                Output("Upload starting...");

                if (ajaxUploadSupported) {
                    AjaxUpload(fileInput[0].files[0]);
                }
                else {
                    //start up persistent connection to server for status checks
                    $.connection.hub.start().done(function () {
                        LegacyUpload(fileInput);
                    });
                }
            }
        }

	    UL.FancyFormSubmit = FormSubmit;
        function CancelUpload() {
            if (ajaxUploadSupported) {
                //TODO: xhr.abort()
            }
            else {
                Dispose();
                $.gritter.add({
                    title: "File Upload Canceled",
                    text: "The upload has been canceled.",
                    image: "/content/img/icons/error-48.png",
                    sticky: true
                });
            }
        }

        function UploadComplete(statusCode, fileName, status) {
            var msg = {};
            $progressPane.filter(".modal-footer").hide();
            progress.parent().removeClass("active");

            if (statusCode == 200) {
                $errorPane.hide();
                progress.addClass("bar-success").removeClass("bar-danger");
                Output(status);
                UpdateProgress(100);//just in case

                msg.title = "File Upload Complete";
                msg.image = "/content/img/icons/start-48.png";
                if (status)
                    msg.text = status;
                else
                    msg.text = "<strong>" + fileName + "</strong> was successfully uploaded.  It may take a minute before your file is available on the site.";

                //close modal on success
                form.closest(".modal").modal("hide");
            }
            else {
                $uploadPane.show();
                $progressPane.hide();
                msg.title = "File Upload Failed";
                msg.image = "/content/img/icons/error-48.png";
                msg.text = "There was an error uploading <strong>" + fileName + "</strong>";
                msg.sticky = true;
                //close modal on failure
                form.closest(".modal").modal("hide");
            }
            $.gritter.add(msg);

            var cid = $.connection.hub.id;
            Dispose();
            Log("closed server connection " + cid);
        }

        function Dispose() {
            $.connection.hub.stop();
            //essentially, this will cancel the current upload
            $uploadIFrame.attr("src", "about:blank");
            //just in case...remove the iframe to prevent IE8 from showing a "save as" dialog for the JSON response
            $uploadIFrame.remove();
        }

        // output file information
        function ParseFile(file) {
            Log(
				"File information: <strong>" + file.name +
				"</strong> type: <strong>" + file.type +
				"</strong> size: <strong>" + file.size +
				"</strong> bytes"
			);
        }

        // ajax upload
        function AjaxUpload(file) {
            var xhr = new XMLHttpRequest();
            if (xhr.upload && file.size <= settings.maxSize) {
                Log("Uploading <strong>" + file.name + "</strong>...");
                UpdateProgress(1);

                // progress bar
                xhr.upload.addEventListener("progress", function (e) {
                    var pc = parseInt((e.loaded / e.total * 90));//90 because there is more work on server after file is uploaded
                    UpdateProgress(pc);
                    //89.8% (31.98 MB of 32.04 MB)
                    Output(pc + "%");
                    Log("XHR.uploadProgress: " + e.loaded + "/" + e.total);
                }, false);

                // file received/failed
                xhr.onreadystatechange = function (e) {
                    if (xhr.readyState == 4) {
                        Log("Complete.  Status: " + xhr.status);
                        UploadComplete(xhr.status, file.name);
                    }
                };

                var formData = new FormData();
                form.serializeArray().forEach(function (field) {
                    formData.append(field.name, field.value);
                });
                formData.append(fileInput.attr("name"), file);

                // start upload
                xhr.open("POST", form.attr("action"), true);
                xhr.send(formData);
            }
            else {
                Output("Not going to upload this file");
            }
        }

        function UpdateProgress(percent) {
            progress.css("width", percent + "%");
        }

        function LegacyUpload(file) {
            var path = file.value;
            var iFrameId = fileselect.attr("id") + "_frame";
            var connectionId = form.find("#UploadId");

            var hubId = $.connection.hub.id;
            connectionId.val(hubId);
            Log("Uploading file with connection " + hubId);

            //
            // Opening an iframe to perform the upload request, essentially, asyncly
            //
            var iframeSrc = "about:blank";
            //var isSecure = /^https/i.test(window.location.href);
            //if (isSecure) iframeSrc = 'javascript:false';//note: this will cause Chrome to open a new window!
            $uploadIFrame = $('<iframe name="' + iFrameId + '" src="' + iframeSrc + '" />');
            var uploadIFrame = $uploadIFrame[0];
            $uploadIFrame.css({ position: 'absolute', top: '-1000px', left: '-1000px' });
            $uploadIFrame.on("load", statusCheck);

            $uploadIFrame.appendTo('body');

            statusCheck.progress = 0;
            function statusCheck(event) {
                try {
                    var doc = getDoc(uploadIFrame);
                    var state = doc.readyState;
                    if (doc.location == null) {
                        Log("iframe disposed");
                    }
                    else if (doc.location.href == iframeSrc) {
                        Log("iFrame uploading... {readyState:" + state + "}");
                    }
                    else {
                        Log("iFrame complete. {readyState:" + state + "}");
                        dispose($uploadIFrame);
                    }
                }
                catch (e) {
                    Log("iframe error: " + e);
                }
            }

            function dispose(frame) {
                frame.off("load", statusCheck);

                frame.remove();
            }

            function getDoc(frame) {
                return frame.contentWindow ? frame.contentWindow.document : frame.contentDocument ? frame.contentDocument : frame.document;
            }

            //
            // repoint form at iframe, submit, and reset it
            //
            function doSubmit() {
                var action = form.attr("action");
                var target = form.attr("target");

                if (!action.endsWith(hubId + ".upl/")) {
                    
                    if (!action.endsWith("/")) {
                        action += "/";
                    }
                    
                    action += hubId + ".upl";
	            }

	            form.attr({
                    //For IE8 you need both
                    encoding: 'multipart/form-data',
                    enctype: 'multipart/form-data',
                    //point form at upload handler (via the extension) and pass in the signalR connection id as the file name
                    action: action
                });

                // tell form to submit data via the iframe
                form.attr("target", iFrameId);

                try {
                    form.submit();
                }
                finally {
                    Log("form.submit() complete");
                    form.attr("action", action);
                    if (target) {
                        form.attr("target", target);
                    }
                    else {
                        form.removeAttr("target");
                    }
                }
            }

            setTimeout(doSubmit, 10);//async call so UI wont be blocked
        }

        // output information
        function Output(msg) {
            $status.html(msg);
        }

        function Log(msg) {
            $("#messages").prepend(new Date().getTime() + ">> " + msg + "<BR />");
            //console.log(msg);
        }


        var fileselect = this;
        var form = this.closest("FORM");
        var $progressPane = $(".progress-pane, .progress-footer", form);
        var $errorPane = $(".error-footer", form);
        var $uploadPane = $(".upload-pane, .upload-footer");
        var progress = $(".progress-pane .bar", form);
        var $status = $(".progress-pane .status-message", form);
        var filedrag = $(settings.dropHole);
        var fileInput = form.find("input[type=file]");
        var $fauxFile = form.find(".faux-file input[type=text]");
        var submitButton = form.find("input[type=submit]");
        var cancelButton = form.find(".cancel");
        var uploadHub = null;//comet connection for legacy browsers
        var $uploadIFrame;
        var fileOperationsSupported = window.File && window.FileList && window.FileReader;

       // file input selection made
        fileselect.change(FileSelectHandler);
        //form.submit(FormSubmit);//too late of an event for IE it seems
        submitButton.on("click", FormSubmit);
        cancelButton.on("click", CancelUpload);

        var ajaxUploadSupported = settings.ajaxUploadEnabled && new XMLHttpRequest().upload;

        if (ajaxUploadSupported) {
            //because jquery is pathetic
            jQuery.event.props.push("dataTransfer");

            if (filedrag) {
                // file drop
                filedrag.on("dragover", FileDragHover);
                filedrag.on("dragleave", FileDragHover);
                filedrag.on("drop", FileSelectHandler);
                //filedrag[0].addEventListener("drop", FileSelectHandler, false);

                //filedrag.style.display = "block";
            }
        }
        else {
            if (filedrag)
                filedrag.remove();

            Log("Ajax upload for this browser is not supported (or not enabled)");

            //
            // map file input to faux file input
            //
            fileInput.change(function () {
                var path = this.value.split('\\');
                $fauxFile.val(path[path.length - 1]);
            });

            //
            // prepare a persistent AJAX connection to server
            //
            uploadHub = $.connection.uploadHub;
            $("#uploadTestBtn").click(function () {
                $.connection.hub.start().done(function () {
                    uploadHub.server.uploadHubTest().done(function () {
                        $.connection.hub.stop();
                    });
                });
            });

            $.extend(uploadHub.client, {
                updateProgress: function (pct, status) {
                    UpdateProgress(pct);
                    Output(status);
                    Log("Hub.updateProgress: " + pct + ", " + status);
                },
                updateStatus: function (status) {
                    //ignore these calls, we will use updateProgress() for good updates and or error() for errors
                    Log("Hub.updateStatus: " + status);
                },
                complete: function (fileName, msg) {
                    UploadComplete(200, fileName, msg);
                    Log("Hub.complete: " + fileName);
                    UL.Refresh();
                },
                error: function (message) {
                    UploadComplete(500, message);
                    Log("Hub.error: " + message);
                }
            });
        }

    };

    //
    // This is used by scratch space upload.
    // Supports multiple files and has both individual and overall progress bars.
    // Uses only Uploadify (Flash based)
    //
    $.fn.multiUpload = function (options) {

        //
        // ensure default settings
        //
        var settings = $.extend({
            maxSize: "125MB",					// The maximum size of an uploadable file in KB (Accepts units B KB MB GB if string, 0 for no limit)
            fileObjName: "File",
            statusArea: null,					// id selector
            progressBar: ".bar",
            statusPane: ".status-message",
            progressPane: ".progress-footer",
            completePane: ".error-footer"
        }, options);


        var $modal = $(settings.statusArea);
        var $progressBar = $(settings.progressBar, $modal);
        var $status = $(settings.statusPane, $modal);
        var $inProgress = $(settings.progressPane, $modal);
        var $complete = $(settings.completePane, $modal);


        function updateProgress(percent) {
            $progressBar.css("width", percent + "%");
        }

        function showProgress() {
            $progressBar.removeClass("bar-danger").removeClass("bar-success");
            $modal.modal("show");
            $inProgress.show();
            $complete.hide();
        }

        function hideProgress() {
            $modal.modal("hide");
        }

        function complete(errorsOccurred) {
            $inProgress.hide();
            $complete.show();

            $modal.on('hidden', function () { window.location.reload(true); });

            if (errorsOccurred)
                $progressBar.addClass("bar-danger").removeClass("bar-success");
            else
                $progressBar.addClass("bar-success").removeClass("bar-danger");
        }

        //
        // create uploadify object
        //
        this.uploadify({
            overrideEvents: ['onUploadProgress', 'onUploadSuccess', 'onUploadComplete', 'onUploadError', 'onSelect'],
            swf: settings.swf,
            fileSizeLimit: settings.maxSize,
            uploader: settings.uploader,
            fileObjName: settings.fileObjName,
            buttonText: settings.buttonText,
            width: 148,
            onDialogOpen: function (queueData) {
                $('body').modalmanager('loading');
            },
            onDialogClose: function (queueData) {
                $('body').modalmanager('loading');
                if (queueData.filesSelected > 0)
                    showProgress();
                else
                    hideProgress();
            },
            onUploadProgress: function (file, bytesUploaded, bytesTotal, totalBytesUploaded, totalBytesTotal) {
                var percent = (totalBytesUploaded / totalBytesTotal) * 100;
                if (totalBytesUploaded == 0)
                    percent = 0;

                $status.html("Uploading <strong>" + file.name + "</strong><BR/>" + Math.round(percent) + '% (' + UL.FormatBytes(totalBytesUploaded) + ' of ' + UL.FormatBytes(totalBytesTotal) + ')');
                updateProgress(percent);
                //alert(this.queueData.queueBytesUploaded);
            },
            onQueueComplete: function (queueData) {
                var good = queueData.uploadsSuccessful;
                var bad = queueData.uploadsErrored;
                var message = good + ' files were successfully uploaded.';
                if (bad > 0)
                    message += "<BR/>" + bad + " files have errors are were not saved.";

                $status.html(message);
                complete(bad > 0);
            }
        });
    };

    //
    // This is used by all container file uploads.
    // Supports multiple files and has both individual and overall progress bars.
    // Uses Uploadify (Flash based for legacy) + Ajax (for modern)
    //
    $.fn.multiUpload2 = function (options) {

        //
        // ensure default settings
        //
        var settings = $.extend({
            maxSize: "125MB",					// The maximum size of an uploadable file in KB (Accepts units B KB MB GB if string, 0 for no limit)
            maxSizeBytes: 131072000,			// just here so we dont have to write code to convert one to the other (UL.FormatBytes() can help us derrive maxSize if we have to)
            maxProgress: 95,					// max progress percentage to show for when all bytes of file are uplaoded.  Since time is spent on server after upload, it is better to not show 100% when its not complete.
            height: 15,
            width: 43,
            fileObjName: "File",
            progressBar: ".bar",
            browsePane: ".browse-pane",
            filePane: ".file-pane",
            metaPane: ".meta-pane",
            fileList: ".files",
            multi: true,
            uploadPane: ".upload-pane",
            completePane: ".complete-footer",
            completeCount: ".completed-count",
            totalCount: ".total-count",
            failedCount: ".failed-count",
            inProgressStatus: ".in-progress",
            uploadButton: ".startUpload",
            cancelButton: ".cancelUpload",
            globalSettings: ".settings",
            buttonImage: null,
            ajaxUploadEnabled: true,
            itemTemplate: null,					// format string to use to generate each upload's UI
            queueId: null,
            uploader: null						// URL to post the uplaod to
        }, options);

        var $this = $(this);
        var $modal = $this.closest(".modal");
        var $browsePane = $(settings.browsePane, $modal);
        var $filePane = $(settings.filePane, $modal);
        var $metaPane = $(settings.metaPane, $modal);
        var $fileList = $(settings.fileList, $modal);
        var $uploadPane = $(settings.uploadPane, $modal);
        var $completePane = $(settings.completePane, $modal);
        var $fileQueue = $("#" + settings.queueId, $modal);

        var $uploadButton = $(settings.uploadButton, $modal);
        var $cancelButton = $(settings.cancelButton, $modal);
        var $fauxFile = $(".faux-file :input", $modal);
        var $completeCount = $(settings.completeCount, $modal);
        var $totalCount = $(settings.totalCount, $modal);
        var $failedCount = $(settings.failedCount, $modal);
        var $inProgressStatus = $(settings.inProgressStatus, $modal);
        var $progressBar = $(settings.progressBar, $modal);
        var $globalSettings = $(settings.globalSettings, $modal);

        var _completedCount, _totalCount, _failedCount, _totalBytesUploaded, _totalBytes = 0, _successFiles = new Array(), _fileQueue = new Array();
        var _fileApiSupported = window.File && window.FileList && window.FileReader;
        var _ajaxUploadEnabled = settings.ajaxUploadEnabled && new XMLHttpRequest().upload && _fileApiSupported;

        initialize();

        function onFilesSelectedFromInput(event) {
            // fetch FileList object, first is from file input, second is from drag/drop
            var files = event.target.files || event.dataTransfer.files;
            var total = files.length;

            //use same structure as uploadify
            var queueData = {
                filesSelected: total,	//The number of files selected in browse files dialog
                filesQueued: 0,			//The number of files added to the queue (that didn’t return an error)
                filesReplaced: 0,		//The number of files replaced in the queue
                filesCancelled: 0,		//The number of files that were cancelled from being added to the queue (not replaced)
                filesErrored: 0,		//The number of files that returned an error (i.e. too large)
                errorMsg: ""
            };

            // process all File objects
            for (var i = 0; i < files.length; i++) {
                parseFile(files[i], queueData);
            }

            // show any errors like uploadify does (we can change both later if we want)
            if (queueData.filesErrored > 0) {
                alert("Some files were not added to the queue:" + queueData.errorMsg);
            }

            queueData.queueLength = queueData.filesQueued;
            onFilesSelected(queueData);
        }

        function parseFile(file, queueData) {
            log(file);
            var itemHtml = settings.itemTemplate;

            if (validateFile(file)) {
                var id = _fileQueue.push(file) - 1;
                file.id = id;//to mirror uploadify's API which is used many places below

                var itemData = {
                    fileID: id,
                    instanceID: $this.id,
                    fileName: file.name,
                    fileSize: UL.FormatBytes(file.size)
                };
                for (var d in itemData) {
                    itemHtml = itemHtml.replace(new RegExp('\\$\\{' + d + '\\}', 'g'), itemData[d]);
                }
                //update UI with item html
                $fileQueue.append(itemHtml);
                //update global counts
                queueData.filesQueued++;
                _totalBytes += file.size;
            } else {
                queueData.errorMsg += '\nThe file "' + file.name + '" exceeds the size limit (' + settings.maxSize + ').';
                queueData.filesErrored++;
            }
        }

        function validateFile(file) {
            return file.size <= settings.maxSizeBytes;
        }

        // when files are selected either from file input or uploadify
        function onFilesSelected(queueData) {
            _completedCount = _failedCount = _totalBytesUploaded = 0;
            _totalCount = queueData.queueLength;
            _successFiles = new Array();

            if (queueData.filesSelected > 0)
                showMetadata();
            //else
            //	hideProgress();
        }

        function showMetadata() {
            $metaPane.show();
            $fileList.show();

            $browsePane.hide();
            $uploadPane.hide();
            $completePane.hide();

            $filePane.css({
                overflow: "hidden",
                height: 0,
                width: 0,
                position: "absolute",
                top: "-1000px",
                left: "-1000px"
            });

            $modal.modal("layout");//re-align modal
        }

        function showUploadPane(event) {
            $progressBar.addClass("bar-success").removeClass("bar-danger").removeClass("bar-warning");
            $inProgressStatus.show();
            $uploadPane.show();

            $metaPane.hide();
            //$browsePane.hide();
            $modal.modal("layout");
        }

        function startAjaxUpload(event) {
            log("start ajax upload");
            log(_fileQueue);
            showUploadPane();

            for (var i = 0; i < _fileQueue.length; i++) {
                uploadFile(_fileQueue[i]);
            }

            return false;
        }

        function uploadFile(file) {
            log("Uploading " + file.name + "...");

            var xhr = new XMLHttpRequest();
            file.xhr = xhr;
            file.loaded = 0;

            updateProgress(1);

            // progress bar
            xhr.upload.addEventListener("progress", function (e) {
                log("XHR.uploadProgress: #" + file.id + "  " + e.loaded + "/" + e.total + " (" + _totalBytesUploaded + "/" + _totalBytes + ")");
                _totalBytesUploaded += (e.loaded - file.loaded);
                //save off upload totals becauase they are different than the size of the file (metadata & http overhead)
                file.loaded = e.loaded;
                file.uploadTotal = e.total;
                var percent = Math.round(e.loaded / e.total * settings.maxProgress);

                //update global progress
                onUploadProgress(file, e.loaded, e.total, _totalBytesUploaded, _totalBytes);

                //update file row's progress
                onUploadFileProgress(file, percent);
            }, false);

            // file received/failed
            xhr.onreadystatechange = function (e) {
                if (xhr.readyState == 4) {
                    log("Upload #" + file.id + " complete with status " + xhr.status);
                    //incase settings.maxProgress is < 100
                    onUploadFileProgress(file, 100);

                    if (xhr.status == 200)
                        onUploadSuccess(file, xhr.responseText, xhr);
                    else if (xhr.status == 0)
                        onUploadError(file, xhr.status, "Upload cancelled", "Upload cancelled");
                    else
                        onUploadError(file, xhr.status, xhr.responseText, xhr.responseText);

                    if (_completedCount == _totalCount)
                        complete(_failedCount > 0);
                }
            };

            var formData = new FormData();
            var data = buildFileMetadata(file);
            for (var prop in data) {
                formData.append(prop, data[prop]);
            }
            formData.append($this.attr("name"), file);

            // start upload
            xhr.open("POST", settings.uploader, true);
            xhr.send(formData);
        }

        function startFlashUpload(event) {
            $this.uploadify('upload', '*');

            showUploadPane();

            return false;
        }

        function buildFileMetadata(file) {
            var data = toObject($("#" + file.id + " :input").serializeArray(), {});
            data = toObject($globalSettings.find(":input").serializeArray(), data);

            return data;
        }

        function toObject(inputs, obj) {
            $.each(inputs, function (i, value) {
                obj[value.name] = value.value;
            });
            return obj;
        }

        function cancelAjaxUpload(file, $element) {
            log("Canceling upload id #" + file.id);

            //remove from file queue so it does not start uploading...
            var index = _fileQueue.indexOf(file);
            if (index > -1)
                _fileQueue.splice(index, 1);

            if (file.xhr) {
                //cancel upload in progress, this event will also trigger the UI to update
                _totalBytesUploaded += (file.uploadTotal - file.loaded);
                file.xhr.abort();
            } else {
                _totalBytesUploaded += file.size;
                onUploadError(file, 0, "Upload cancelled", "Upload cancelled");
                //$element.closest(".uploadify-queue-item").fadeOut(500, function () { $(this).remove(); });
            }

            onUploadProgress(file, 0, file.size, _totalBytesUploaded, _totalBytes);
        }

        function onUploadProgress(file, bytesUploaded, bytesTotal, totalBytesUploaded, totalBytesTotal) {
            //if a file errors, uploadify will not include it in totalBytesUploaded
            //so we will save off progress to ensure we never reduce our overall progress
            _totalBytesUploaded = Math.max(_totalBytesUploaded, totalBytesUploaded);
            var percent = (_totalBytesUploaded / totalBytesTotal) * settings.maxProgress;
            if (totalBytesUploaded == 0)
                percent = 0;

            //$statusPane.html("Uploading <strong>" + file.name + "</strong><BR/>" + Math.round(percent) + '% (' + UL.FormatBytes(totalBytesUploaded) + ' of ' + UL.FormatBytes(totalBytesTotal) + ')');
            updateProgress(percent);
            log("Upload percent " + percent);
        }

        function onUploadFileProgress(file, percent) {
            $('#' + file.id).find('.data').html(' - ' + percent + '%');
            $('#' + file.id).find('.uploadify-progress-bar').css('width', percent + '%');
        }

        function updateProgress(percent) {
            if (percent)
                $progressBar.css("width", percent + "%");

            $completeCount.text(_completedCount);
            $totalCount.text(_totalCount);
            $failedCount.text(_failedCount);
            $failedCount.parent()[_failedCount > 0 ? "show" : "hide"]();
        }

        function complete(errorsOccurred) {
            updateProgress(100);
            $inProgressStatus.html("Upload complete!");

            $browsePane.hide();
            $uploadPane.filter(".modal-footer").hide();

            $completePane.show();
            //$modal.modal("layout");

            if (errorsOccurred)
                $progressBar.addClass("bar-danger").removeClass("bar-success").removeClass("bar-warning");
            else {
                $progressBar.addClass("bar-success").removeClass("bar-danger").removeClass("bar-warning");
                var goodFile = _successFiles[0] || { name: "Unknown" };
                var message = _completedCount > 1
    				? "<strong>" + goodFile.name + "</strong> and " + (_completedCount - 1) + " other files were added to your documents and will be available shortly."
    				: "<strong>" + goodFile.name + "</strong> was added to your documents and will be available shortly.";
                $.gritter.add({
                    title: "Files Uploaded",
                    text: message
                });
                $modal.modal("hide");

                UL.Refresh();
            }
        }

        function onUploadSuccess(file, data, response) {
            //our server will ALWAYS return a 200 response with a JSON object body
            //even if errors occur, it will have two properties, success and message.
            //we do this so that we are able to transmit message to the client (uploadify doesnt give us what we need when it errors)
            var result = $.parseJSON(data);
            if (result.success) {
                _completedCount++;
                updateProgress();
                $("#" + file.id + " I").attr("class", "ul-icon-success");
                $("#" + file.id + " .close").remove();
                _successFiles.push(file);
            } else {
                onUploadError(file, 500, result.message, result.message);
            }
            log('The file ' + file.name + ' was successfully uploaded with a response of ' + response + ':' + data);
        }

        function onUploadError(file, errorCode, errorMsg, errorString) {
            _completedCount++;
            _failedCount++;
            updateProgress();

            var uploadStatusPane = $("#" + file.id);
            uploadStatusPane.addClass("uploadify-error");
            uploadStatusPane.find("I").attr("class", "ul-icon-failure");
            uploadStatusPane.find(".data").html(" - " + errorString);
            $progressBar.removeClass("bar-success").addClass("bar-warning");
            log('The file ' + file.name + ' could not be uploaded: ' + errorString);
        }

        function log(message) {
            //console.log(message);
            //alert(message);
        }

        function getFileById(id) {
            for (var i = 0; i < _fileQueue.length; i++) {
                if (_fileQueue[i].id == id)
                    return _fileQueue[i];
            }
            return null;
        }

        function initialize() {
            //disable the fake file input
            $fauxFile.on("focus click select", function () {
                this.blur();
                return false;
            });

            //cancel all buttons
            $cancelButton.on("click", function (event) {
                event.preventDefault();//do now...incase there are exceptions

                if (_ajaxUploadEnabled) {
                    for (var i = _fileQueue.length - 1; i >= 0; i--) {
                        cancelAjaxUpload(_fileQueue[i]);
                    }
                    updateProgress(100);
                } else {
                    $this.uploadify("cancel", "*");
                }

                return false;
            });

            //cancel single file buttons
            $fileQueue.on("click", ".close", null, function (event) {
                var fileId = $(this).data("fileId");

                if (_ajaxUploadEnabled)
                    cancelAjaxUpload(getFileById(fileId), $(this));
                else
                    $this.uploadify('cancel', fileId);
            });

            //dispose when dialog is closed
            $modal.on('hidden', function () {
                if (!_ajaxUploadEnabled) {
                    $this.uploadify("destroy");
                }
                //file-level progress bars wont work the second time unless we delete the previous ones
                $fileList.remove();
            });


            //
            // hook up either AJAX or Flash based upload events
            //
            if (_ajaxUploadEnabled) {
                log("using ajax upload");

                //fixes drag & drop events (stupid jquery!)
                jQuery.event.props.push("dataTransfer");

                //hook file selections, upload button, and file cancel
                $this.change(onFilesSelectedFromInput);
                $uploadButton.on("click", startAjaxUpload);
            } else {
                log("using flash upload");

                //
                // hook the upload button, and create the Uploadify flash object
                //
                $uploadButton.on("click", startFlashUpload);
                $this.uploadify({
                    overrideEvents: ['onUploadComplete'],
                    swf: settings.swf,
                    fileSizeLimit: settings.maxSize,
                    height: settings.height,
                    width: settings.width,
                    uploader: settings.uploader,
                    fileObjName: settings.fileObjName,
                    buttonText: settings.buttonText,
                    button_image_url: settings.buttonImage, // to fix uploadify...otherwise it will just request ./ causing a 500
                    auto: false,
                    multi: settings.multi,
                    queueID: settings.queueId,
                    itemTemplate: settings.itemTemplate,
                    onDialogOpen: function (queueData) {
                        $('body').modalmanager('loading');
                    },
                    onDialogClose: function (queueData) {
                        $('body').modalmanager('loading');
                        onFilesSelected(queueData);
                    },
                    onUploadStart: function (file) {
                        var data = buildFileMetadata(file);

                        log("onUploadStart\n" + $.param(data));
                        $this.uploadify("settings", "formData", data);
                    },
                    onUploadSuccess: onUploadSuccess,
                    onUploadError: onUploadError,
                    onUploadProgress: onUploadProgress,
                    onQueueComplete: function (queueData) {
                        //we cant trust uploadify's queue data because we return errors as 200's
                        //see onUploadSuccess() above for more info
                        complete(_failedCount > 0);
                    }
                });
            }
        }
    };

})(jQuery);


//
// common modal functions
//
(function ($) {
    //
    // Should be bound to the click event of a delete button on the "are you sure" delete modal.
    // This will spawn a modal to actually perform the delete (with loading indicator) to the URL specified
    // by the data-item-href attribute.  On success, it will show a gritter (if specified) and refresh the
    // page using the data-action-redirect attribute (if specified - or just refresh the page if not).
    //
    UL.ModalDelete = function (event) {
        var $modal = $(this).closest(".modal").modal("loading");
        var successRedirect = $(this).data("actionRedirect");

        $.ajax({
            type: 'POST',
            url: $(this).data('itemHref'),
            cache: false,
            success: function (result) {
                if (result.message)
                    $.gritter.add(result.message);

                if (result.success) {
                    if (successRedirect)
                        window.location = successRedirect;
                    else if (!result.noRefresh)
                        UL.Refresh();
                }
                //close modal regardless of refresh or error
                $modal.modal("hide");
            },
            error: function (jqXhr, status, error) {
                $.gritter.add({ title: status || "Error", text: error, sticky: true });
            },
            complete: function () {
                try { $modal.modal("removeLoading"); } catch (ex) { }
            }
        });
    };

})(jQuery);

$.ajaxPrefilter("json script", function (options, originalOptions, jqXHR) {
    // we have correct HTTP Cache headers set up - so force jQuery to respect them for 
    // scripts returned from AJAX requests (which otherwise would ignore the cache:true we tell it to use...sigh)
    options.cache = true;
});

$(document).ready(function () {


    UL.ModelLinkHandler = function (event) {
        $('body').modalmanager('loading');
        var $modal = UL.CreateModalChrome(this);

        $.ajax({
            type: "GET",
            url: this.href,
            cache: true,
            success: function (data) {
	           try {
		           $modal.html(data);
		           $modal.modal();
	           } catch (ex) {
		        var $html = UL.CreateModal("Error", "<p> After Ajax Call" + ex.message + "</p>");
	           	$modal.html($html);
	           	$modal.modal();
	           }
            	
            },
            error: function (jqXHR, textStatus, errorThrown) {
                var title = textStatus || "Error";
                var body = errorThrown || "An unexpected error has occurred.";

                try {
	                var errorPage = $(jqXHR.responseText.trim());
	                title = errorPage.find("h1").text() || title;
	                body = errorPage.find(".body-content").html() || errorPage.find("h2").text() || body;

                } catch (ex) {
	               
                }

                var $html = UL.CreateModal(title, "<p>" + body + "</p>");
                $modal.html($html);
                $modal.modal();
            }

        });
        return false;
    }


    $("body").on("click", "a[data-toggle='modal'][href!='#']", UL.ModelLinkHandler);


    $('a[data-toggle="modal"][data-target="#DeleteModal"]').on('click', function () {
        if ($(this).data('modalTitle'))
            $('#DeleteConfirmModalLabel').html($(this).data('modalTitle'));

        $('#delete_modal_body').html("<p>" + $(this).data('itemName') + "</p>");

        $('button#submitDelete').data($(this).data());
    });

    $('button#submitDelete').on('click', UL.ModalDelete);

});

(function($) {

	var buttonKeys = { "EnterKey": 13 };
	$('#frmSearchTop').keypress(function(e) {
		if (e.which == buttonKeys.EnterKey) {
			var defaultButtonId = $(this).attr("defaultbutton");
			$("#" + defaultButtonId).click();
			return false;
		}
	});
	$('.focus :input:first').focus();
})(jQuery);


(function ($) {
	$.extend({
		getQueryString: function (name) {
			function parseParams() {
				var params = {},
                    e,
                    a = /\+/g,  // Regex for replacing addition symbol with a space
                    r = /([^&=]+)=?([^&]*)/g,
                    d = function (s) { return decodeURIComponent(s.replace(a, " ")); },
                    q = window.location.search.substring(1);

				while (e = r.exec(q))
					params[d(e[1])] = d(e[2]);

				return params;
			}

			if (!this.queryStringParams)
				this.queryStringParams = parseParams();

			return this.queryStringParams[name];
		}
	});
})(jQuery);

(function($) {
	$('.addToFavorites').click(function () {
		$('body').modalmanager('loading');
		var $modal = UL.CreateModalChrome(this);
		var url = $(this).attr("data-item-href"); // + "?" + $.param(args);
		$.ajax({
			type: "GET",
			url: url,
			data: $('#frmSearchTop').serialize(),
			cache: false,
			success: function (data) {
				$modal.html(data);
				$modal.modal();
			}
		});
		return false;
	});


})(jQuery);

(function($) {
if (typeof String.prototype.endsWith !== 'function') {
	String.prototype.endsWith = function (suffix) {
		return this.indexOf(suffix, this.length - suffix.length) !== -1;
	};
}
})(jQuery);