/// <reference path="../_references.js" />
/// <reference path="../_ULReferences.js" />

(function ($) {
    'use strict';
    if (!window.UL) {
        window.UL = {};
    }

    var MonitorClass = function () {
        /// <summary>
        /// Memory Monitor (Lock)
        /// Provides memory lock functionallity similar to .NET
        /// however, it locks on an object but to be thread safe in JS,
        /// changes to the data object you are trying to protect must be done in 
        /// a delegate method supplied to the monitor upon entering a lock state.
        /// </summary>
        /// <remarks>
        /// Typical usage:
        /// 
        ///   var Static_Readonly_Global_pad_lock = {};
        ///   UL.Utility.safeFreeze(clm_Pad_Lock);  
        /// 
        ///   UL.Monitor.Enter(clmLock, function (arg1, arg2) {
        ///         //CAll your code
        ///         //optional condition to exit monitoring 
        ///         if(YourCondition) {
        ///             UL.Monitor.Exit(clmLock);   <--(optional)
        ///         }
        ///    }, [arg1, arg2])
        /// 
        /// </remarks>

        this.newGuid = function () {
        	/// <summary>
        	/// Generates a new Globally Unique identifier (GUID)
        	/// </summary>
        	/// <returns type="Guid">String in Guid format</returns>
            var d = new Date().getTime();
            var uuid = 'xxxxxxxxxxxx4xxxyxxxxxxxxxxxxxxx'.replace(/[xy]/g, function (c) {
                var r = (d + Math.random() * 16) % 16 | 0;
                d = Math.floor(d / 16);
                return (c === 'x' ? r : (r & 0x7 | 0x8)).toString(16);
            });
            return uuid;
        };

        this.defaultTimeout = 10 * 1000; //10 sec

        var lockedObjects = [];

        var lockWaitQueues = [];

        this.workingLockOn = false;

        this.Enter = function (obj, timeout, delegate, delegateArgs) {
        	/// <summary>
        	/// Allows code to enter into a monitored lock state on the specified lock object.
        	/// </summary>
        	/// <param name="obj">The lock object, same purpose to the static readonly pad_lock object in c#</param>
        	/// <param name="timeout" type="Number">timeout period in seconds</param>
        	/// <param name="delegate" type="Function">Function to be executed when request receives lock token.</param>
            /// <param name="delegateArgs">Arguments to be sent to the delegate function</param>
            var self = this;
            if (!this.workingLockOn) {
                try {
                    this.workingLockOn = true;

                    if (!timeout) {
                        timeout = this.defaultTimeout;
                    }

                    var curLockedObj = lockedObjects.find(function (x) { return x.obj === obj; });
                    if (curLockedObj) {
                        lockWaitQueues[curLockedObj.key].push({ del: delegate, args: delegateArgs, timeout: timeout });
                    }
                    else {

                        var lockId = this.newGuid();
                        lockedObjects.push({
                            key: lockId,
                            obj: obj,
                            releaseTimer: setTimeout(this.Exit, timeout, [obj])
                        });

                        lockWaitQueues[lockId] = [];
                        setTimeout(function(){

                            delegate.apply(null, delegateArgs);
                            UL.Monitor.Exit(obj);

                        }, 1);
                    }
                }
                catch (ex) {
                    throw ex;
                }
                finally {
                    this.workingLockOn = false;
                }

            }
            else {
                setTimeout(function () {

                    self.Enter(obj, timeout, delegate, delegateArgs);
                }, 100);
            }

        };

        this.Exit = function (obj) {
        	/// <summary>
        	/// Exists the monitored lock on the specified object.
        	/// </summary>
            /// <param name="obj" type="Object">The lock object</param>

            if (!this.workingLockOn) {
                try {
                    this.workingLockOn = true;

                    var curLockedObj = lockedObjects.find(function (x) { return x.obj === obj; });

                    if (curLockedObj) {

                        clearTimeout(curLockedObj.releaseTimer);
                        var next = lockWaitQueues[curLockedObj.key].pop();
                        if (next) {
                            curLockedObj.releaseTimer = setTimeout(this.Exit, curLockedObj.obj);
                            setTimeout(function () {
                                next.del.apply(null, next.args);
                                UL.Monitor.Exit(next.obj);
                            }, 1);
                        }
                        else {
                            delete lockWaitQueues[curLockedObj.key];
                            lockedObjects.remove(curLockedObj);
                        }
                    }
                }
                catch (ex) {
                    throw ex;
                }
                finally {
                    this.workingLockOn = false;
                }
            }
            else {
                setTimeout(this.Exit, 100, [obj]);
            }

        };
    };

    //do not allow changes to this object and runtime.  
    if (Object.freeze) {
        Object.freeze(MonitorClass);       
    }

    UL.Monitor = new MonitorClass();

    //this is the monitor test harness
    //to test add a button tag with the class "thread-test-btn"
    //************ MONITOR TEST HARNESS BEGIN *************
    var pad_lock = {};

    var runTestProcess = function (id, arg1, arg2) {

        //setup
        var testDelegate = function (xid, xarg1, xarg2) {

            var endLoopTime = Date.now();
            endLoopTime = endLoopTime.setSeconds(endLoopTime.getSeconds() + 5);
            window.alert("start wait" + xid);

            /*jslint unparam: true*/
            var y = 0;
            while (Date.now() < endLoopTime) {
                //do nothering -- test delay.
                y++;
                if (y % 100 === 0) {
                    window.console.debug("Testing: blooking loop running.");
                }
            }
            window.alert("stop wait" + xid);
            UL.Monitor.Exit(pad_lock);
        };

        //run
        UL.Monitor.Enter(pad_lock, 10000, testDelegate, [id, arg1, arg2]);
     
    };
    $(".thread-test-btn").click(function (e) {
          runTestProcess(UL.Monitor.newGuid(), 23, 44);
    });
 

}(jQuery));