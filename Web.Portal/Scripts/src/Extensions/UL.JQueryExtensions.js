/// <reference path="../_references.js" />
(function ($) {

    'use strict';

    $.fn.htmlAll = function () {
        /// <summary>
        /// Returns All the html for the fist element in the jquery object 
        /// inlcluding the elements tag and all inner html.
        /// </summary>
        /// <returns type="">html string</returns>
        if (this[0] === 'undefined' || this[0] === null) {
            return '';
        }

        var html = this[0].outerHTML || "";
        return html.replace("\r\n", "");
    };

    (function ($) {

        // this is like doing an override with 
        // inheritiance on a method in c#
        var oldClean = $.cleanData;

        $.cleanData = function (elems) {
            /// <summary>
            /// Extends the jquery cleanData function to expose the a destroyed event
            /// Event is to be used to help widgets clean themselves up.
            /// </summary>
            /// <param name="elems">Elements to attach a destroy trigger.</param>

            var i = 0;
            var elem = null;
            for (i = 0; i < elems.lenght; i++) {

                elem = elems[i];

                if (elem !== undefined) {
                    $(elem).triggerHandler("destroyed");
                }
                else {
                    break;
                }

            }
            oldClean(elems);
        };

    }($));



    $.fn.fillParentHeight = function () {
        /// <summary>
        /// Adjusts the jqQuery results objects height to match the parents height
        /// NOTE: this should be run last if other natural control flow may adjust 
        /// the parents height. This is not a binder but a matcher.
        /// </summary>
        $(this).each(function () {
            var parents = $(this).parents("td");
            var parent = $(parents[0]);
            if (parent) {
                $(this).height(parent.innerHeight() - 6);
            }
        });
    };


    $.fn.ellipsis = function () {
        /// <summary>
        /// Turns long text into an ellipsis
        /// it uses the data api for the following properties.
        /// data-ellipsis-width and data-ellipsis-height.
        /// </summary>
        $(this).each(function () {

            var h = $(this).data("ellipsisHeight");
            var w = $(this).data("ellipsisWidth");

            if (w) {
                $(this).width(w);
            }

            if (h) {
                $(this).height(h);
            }

            $(this).dotdotdot({
                ellipsis: '... ',
                wrap: 'letter'
            });
        });
    };


    $.fn.ellipsisAdd = function () {
        /// <summary>
        /// Takes a slice of a string, adds an ellipsis, and then reinserts into the DOM.
        /// Place data-max-chars attribute with integer value inline to customize number of characters to display.
        /// Allow three characters for the ellipsis.
        /// </summary>

        try {
            // Determines crumb level
            var numCrumbs = $(this).length;

            $(this).each(function(index, elem) {
                var elemData = $(elem).data();
                var thisClass = $(this).prop('class');

                if (elemData !== null) {
                    var thisElem = $(elem);
                    var titleNameText = thisElem.text();
                    var titleNameTextSubstr;
                    var maxChars = 20; // Default value
                    var thisParentLi; 

                    if (elemData.maxChars !== undefined) {
                        maxChars = elemData.maxChars - 3; // allow for ellipsis

                        if (titleNameText.length > maxChars) {
                            // For everything but breadcrumbs
                            if (thisClass !== 'breadcrumb-style ellipsis-add') {
                                titleNameTextSubstr = titleNameText.substr(0, maxChars - 3);
                                $(this).text(titleNameTextSubstr + '...');
                            }
                            // For breadcrumb project name on overview page. Allows it to go full width of page. 
                            else if (thisClass === 'breadcrumb-style ellipsis-add' && numCrumbs === 4) {

                                titleNameTextSubstr = titleNameText.substr(0, maxChars + 30);
                                thisParentLi = thisElem.closest('li');
                                thisParentLi.css('maxWidth', '610px');
                                $(this).addClass('longBreadcrumb');
                                $(this).text(titleNameTextSubstr + '...');
                            }
                            // For breadcrumb project name on drilldown pages. Cuts it short to allow for additional levels. 
                            else if (thisClass === 'breadcrumb-style ellipsis-add' && numCrumbs > 4) {
                                thisParentLi = thisElem.closest('li');
                                thisParentLi.addClass('section-crumbs-li');
                                titleNameTextSubstr = titleNameText.substr(0, maxChars - 3);
                                $(this).text(titleNameTextSubstr + '...');
                            }
                        }
                    } 
                } else {
                    window.console.log("Element data\'s value is null");
                }
            });
        } catch (e) {
            window.console.log(e.name + ": " + e.message);
        }
    };


    $.fn.disable = function () {
        /// <summary>
        /// 
        /// </summary>
        /// <returns type="jQuery"></returns>

        $(this).each(function (index, elem) {
            if (!$(elem).is(":disabled")) {
                $(elem).attr("disabled", "disabled");
            }
        });

        return this;
    };

    $.fn.enable = function () {
        /// <summary>
        /// 
        /// </summary>
        /// <returns type="jQuery"></returns>

        $(this).each(function (index, elem) {
            $(elem).removeAttr("disabled");
        });
        return this;
    };

    $.fn.check = function () {
        /// <summary>
        /// 
        /// </summary>
        /// <returns type="jQuery"></returns>

        $(this).each(function (index, elem) {
            if (!$(elem).is(":checked")) {
                $(elem).prop("checked", true);
            }

        });
       

        return this;
    };

    $.fn.uncheck = function () {
        /// <summary>
        /// 
        /// </summary>
        /// <returns type=""></returns>

        $(this).each(function(index, elem){
            $(elem).prop("checked", false);
        });
        return this;
    };

    $.fn.trimOn = function (eventName) {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="eventName"></param>
        /// <returns type="jQuery"></returns>

        $(this).on(eventName, function (e, ui) {
            $(this).val($(this).val().trim());
        });

        return this;
    };


    $.fn.toggleControlOn = function (eventName) {
        /// <summary>
        /// 
        /// </summary>
        /// <returns type="jQuery"></returns>

        $(this).on(eventName, function (e, ui) {
            var enable = $(this).val().toLowerCase() === "true";
            var controlToToggle = $($(this).data("target"));
            if (enable) {
                controlToToggle.enable();
                return;
            }

            controlToToggle.disable();
        });

        return this;
    };

    $.fn.toggleControlOnMatch = function (eventName) {
    	/// <summary>
    	/// 
    	/// </summary>
    	/// <returns type="jQuery"></returns>

    	$(this).on(eventName, function (e, ui) {
    		var enable = $(this).val().toLowerCase() === $(this).data("targetvalue").toLowerCase();
    		var controlToToggle = $($(this).data("target"));
    		if (enable) {
    			controlToToggle.enable();
    			return;
    		}

    		controlToToggle.disable();
    	});

    	return this;
    };

    $.fn.toggleCheckAllOn = function (eventName) {
    	/// <summary>
    	/// 
    	/// </summary>
    	/// <param name="eventName"></param>
        /// <returns type=""></returns>

        $(this).on(eventName, function (e, ui) {
            var groupSelector = $(this).data("groupSelector");
            if ($(this).is(":checked")) {
                $(groupSelector).check();
            }
            else {
                $(groupSelector).uncheck();
            }

        });

        return this;
    };

    $.fn.isMarkedVisible = function () {
    	/// <summary>
        /// Returns a value indicating where or not the 
        /// item would be visible, disregarding attachment to the DOM.        
    	/// </summary>
        /// <returns type=""></returns>

        var self = $(this);
        return self.css("display").toLowerCase() !== "none";            
    };


}(jQuery));

// jQuery plugin to prevent double submission of forms 
// Register the form with "$('form').preventDoubleSubmission();"
// If you use on AJAX forms you will have to re-enable the button on any error returns.
jQuery.fn.preventDoubleSubmission = function () {
    'use strict';
    $(this).on("submit", function (e) {
        var $form = $(this);

        if ($form.data("submitted") === true) {
            // Previously submitted - don't submit again
            e.preventDefault();
        } else {
            // Mark it so that the next submit can be ignored
            $form.data("submitted", true);
            $form.find("input[type=submit]").attr("disabled", "disabled");
        }
    });
    // Keep chainability
    return this;
};