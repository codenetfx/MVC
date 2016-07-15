/// <reference path="../_references.js" />
/// <reference path="../_ULReferences.js" />
/// <reference path="../../Lib/jquery/plugins/idleTimer.js" />

//Session manager
(function ($) {
    'use strict';
    if (!window.UL) {
        window.UL = {};
    }

    UL.sessionManager = function (options, elem) {
        /// <summary>
        /// The session manager provides client session timeout warnings 
        /// and session refresh pings to the server while there is user interaction with the specified htmlObject.
        /// Note: It will only ping the server if user interaction has occured with in the specified interval.
        /// </summary>
        /// <param name="options" type="Object">configuration options object.</param>
        /// <param name="elem" type"HtmlObject">The HTML object for whitch to track user integraction.</param>
        /// <returns type="SessionManager">reference to SessionManager Class instance.</returns>

        this.jqTarget = $(elem);
        this.options = options;
        this.serverCallTimer = null;
        this.lastCallToServer = Date.now();
        this.init();
        return this;
    };

    UL.sessionManager.prototype = {

        init: function () {
            /// <summary>
            /// Private: initializtion method.
            /// </summary>
            var self = this;

            //if the target obj is removed /destroy the timer;
            if (self.jqTarget[0] !== document) {
                self.jqTarget.on("destroyed", function (e) {
                    self.destroy();
                });
            }

            self.jqTarget = self.jqTarget.idleTimer({
                timeout: self.options.idleWarningTimeout
            });

            self.jqTarget.on("idle.idleTimer", this.getIdleHandler());
            self.jqTarget.on("active.idleTimer", this.getActiveHandler());
            this.startServerCallTimer();
        },
        start: function () {
            /// <summary>
            /// Starts watching user integraction. 
            /// </summary>

            var self = this;
            self.jqTarget.idleTimer("resume");
            self.startServerCallTimer();
        },

        stop: function () {
            /// <summary>
            /// Stops user interaction watching. 
            /// </summary>

            var self = this;
            self.jqTarget.idleTimer("pause");
            self.startServerCallTimer();
        },

        startServerCallTimer: function () {
            var self = this;

            if (this.serverCallTimer !== null) {
                this.stopServerCallTimer();
            }

            if (this.serverCallTimer === null) {

                this.serverCallTimer = setInterval(function () {
                    self.refreshSession();
                }, this.options.serverCallInterval);
            }
        },

        stopServerCallTimer: function () {
            /// <summary>
            /// Private: stops the call timer, which controls the periodic ping to the server.
            /// </summary>

            clearInterval(this.serverCallTimer);
            this.serverCallTimer = null;
        },

        getActiveHandler: function () {
            /// <summary>
            /// Gets the event handler function to be run when the user starts 
            /// interacting with the specified html Object.
            /// </summary>
            ///<retuns type="Function(e)">Retuns the event handler.</returns>
            var self = this;

            return function (e) {
                /// <summary>
                /// When the user becomes active again, check to see when the last ping occured, 
                /// if min interval has passsed, ping the server then start the call timer.
                /// </summary>
                /// <param name="e" type="Event">jquery event object</param>

                if ((Date.now() - self.lastCallToServer) > self.options.serverCallInterval) {
                    self.refreshSession();
                }

                self.startServerCallTimer();
            };
        },

        getIdleHandler: function () {
            /// <summary>
            /// Returns the Idle event handler.
            /// </summary>
            ///<retuns type="Function(e)">Retuns the event handler.</returns>

            var self = this;
            var msg = "The system has detected user inactivity."
            + " If this continues your session will time out and any changes will be lost.";
            var title = "Inactivity Warning";

            return function (e) {
                /// <summary>
                /// When the user becomes iddle, stop pinging the server and alert 
                /// the user that their session will timeout for inactivity.
                /// </summary>
                /// <param name="e"></param>

                self.stopServerCallTimer();
                UL.ShowAlert(title, msg);
            };
        },

        refreshSession: function () {
            /// <summary>
            /// Sends a ping to the server to extend the session.
            /// </summary>

            var self = this;
            var canRefresh = self.jqTarget !== null
                && self.jqTarget.length > 0
                && self.jqTarget.is(":visible");

            if (canRefresh) {
                $.ajax({
                    url: "/profile/JsKeepAlive",
                    type: 'GET',
                    success: function (data) {
                        if (data) {
                            self.lastCallToServer = Date.now();
                            //window.alert("ping server");
                            return true;
                        }
                    },
                    error: function (data) {
                        window.alert("Your session was unable to be extended.\r\nThe page must be refreshed, any unsaved data will be lost.");
                        window.location.reload();
                    }
                });
            }
            else {
                self.destroy();
            }
        },

        destroy: function () {
            /// <summary>
            /// disposes of the Session Manager Object.
            /// </summary>

            var self = this;
            self.jqTarget.idleTimer("destroy");
            delete self.jqTarget; // detach ref to html elem
            self.stopServerCallTimer();
        }
    };

    $.fn.sessionManager = function (options, optionArgs) {
        /// <summary>
        /// The session manager provides client session timeout warnings 
        /// and session refresh pings to the server while there is user interaction with the specified htmlObject.
        /// Note: It will only ping the server if user interaction has occured with in the specified interval.
        /// </summary>
        /// <param name="options" type="Object/String" optional="true">When type is an object used as the configuration options, when is a string,
        ///  options is tread as the method name to be called if optionsArgs is supplied it is passed to the method.</param>  
        /// <param name="optionArgs" type="Object" optional="true">When calling the SessionManager api method, 
        /// used as the argument to be passed to the method.</param>    
        /// <returns type="jqResult">the jQuery Result object.</returns>

        if (this[0]) {
            if (typeof options === 'string') {

                var smObj = $(this[0]).data("sessionManager");
                if (smObj !== null) {
                    try {
                        smObj[options](optionArgs);
                    }
                    catch (e) {
                        throw options.toString() + " was an invalid behavior.";
                    }
                }
            }
            else {
                $(this[0]).data("sessionManager", new UL.sessionManager(options, this[0]));
                return $(this[0]);
            }
        }

        return this;
    };
}(jQuery));