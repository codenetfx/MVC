/// <reference path="../_references.js" />
/// <reference path="../_ULReferences.js" />
/// <reference path="../../Lib/jquery/plugins/jquery.signalR-1.0.1.js" />


(function () {
    'use strict';
    if (!window.UL) {
        window.UL = {};
    }

    //*************** Stream Manager ******************
    UL.StreamManager = function () {
        /// <summary>
        /// 
        /// </summary>

        this.hub = null;
        this.conn = null;
        this.clientId = null;
        this.dataRecievedDelegate = null;
        this.init();
    };

    UL.StreamManager.prototype = {
        init: function () {
            /// <summary>
            /// Private: used to initilize the StreamManager.
            /// </summary>

            var self = this;
            this.hub = $.connection.jsonStreamingHub;
            this.hub.client.receive = function (data) {
                if (self.dataRecievedDelegate
                    && typeof (self.dataRecievedDelegate) === 'function') {
                    self.dataRecievedDelegate(data);
                }
            };
            this.conn = $.connection.hub;
            this.conn.received(this.getReceivedHandler());
            this.conn.error(this.getErrorHandler());
            this.conn.stateChanged(this.getStateChangedHandler());
            this.conn.reconnected(this.getReconnectHandler());
            this.conn.starting(this.getStartingHandler());
        },

        getReconnectHandler: function () {
            /// <summary>
            /// Returns an event handler for socket reconnection
            /// </summary>

            return function () {
                window.console.log("StreamManager: reconnected");
            };
        },
        getErrorHandler: function () {
            /// <summary>
            /// returns an event handler for Error handling
            /// </summary>

            return function (e1, e3) {
                window.console.log("StreamManager: error occurred");
            };
        },
        getReceivedHandler: function () {
            /// <summary>
            /// Returns an event handler for when communication is Received
            /// </summary>
            return function (e1, e2) {
                window.console.log("StreamManager:  received data");
            };
        },
        getStateChangedHandler: function () {
            /// <summary>
            /// Returns an event handler that is executied when the connection state changes.
            /// </summary>

            return function (state) {
                window.console.log("StreamManager: connection state changed");
            };
        },
        getStartingHandler: function () {
            /// <summary>
            /// Returns an event handler to be invoked when a connection is started.
            /// </summary>

            return function () {
                window.console.log("StreamManager: connection starting");
            };
        },
        getConnEstablishedHandler: function () {
            /// <summary>
            /// Returns an event handler that is invoked when a socket connection is established with the server.
            /// </summary>

            var self = this;

            return function () {
                window.console.log("StreamManager: connection established");
                self.clientId = $.connection.hub.id;
            };
        },
        connect: function () {
            /// <summary>
            /// initiates a socket connection to the server.
            /// </summary>
            /// <returns type=""></returns>

            return this.conn.start(this.getConnEstablishedHandler());

        },
        startRequest: function (url, requestData, dataReceivedCallback, requestCompletedCallback) {
            /// <summary>
            /// Starts a request to the specified url using the requestData as post information.
            /// </summary>
            /// <param name="url">The desitination of the request.</param>
            /// <param name="requestData">data to be posted to the url</param>
            /// <param name="dataReceivedCallback" type="Function">A function that is called when the 
            /// request is successfully complete with data.</param>
            /// <param name="requestCompletedCallback" type="Function">A function that is call 
            /// once the request process is competed.</param>

            var self = this;
            self.startBlocking();
            self.dataRecievedDelegate = dataReceivedCallback;
            //if connect is active           
            requestData.ClientId = self.clientId;

            $.ajax({
                type: 'POST',
                contentType: 'application/json',
                url: url,
                data: JSON.stringify(requestData),
                success: function (data) {
                    if (data && data.Successful) {
                        self.conn.stop();
                        if (requestCompletedCallback
                            && typeof requestCompletedCallback === 'function') {
                            requestCompletedCallback(data);
                        }
                    }
                },
                /* jslint */
                complete: function (e) {
                    self.stopBlocking();
                },
                error: function (jqXHR, textStatus, errorThrown) {
                	UL.HandleAjaxUnhandledError(jqXHR, textStatus, errorThrown);
                }
            });
        },
        startBlocking: function () {
            var blockOptions = {
                message: UL.loadAnimationLarge,
                baseZ: 11000,
                overlayCSS: {
                    backgroundColor: '#000',
                    opacity: 0.5,
                    cursor: 'wait'
                },
                css: {
                    border: 'none',
                    backgroundColor: 'transparent'
                },
                centerX: false,
                centerY: false
            };

            $('body').block(blockOptions);
        },
        stopBlocking: function () {
            $('body').unblock();
        }
    };

    $(".thread-test-btn").click(function (e) {
        var sm = new UL.StreamManager();
        sm.connect().done(function () {
            sm.startRequest("/company/GetAccounts", function (data) {
                return;
            });
        });
    });

}(jQuery));






