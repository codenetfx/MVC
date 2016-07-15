/// <reference path="../_references.js" />
/// <reference path="../../Lib/jquery/plugins/deep-diff.js" />

(function ($) {
    "use strict";
    window.UL = {};
    UL.storage = $.initNamespaceStorage('UL').localStorage;
    UL.loadAnimationLarge = $('<div class="loading-spinner" style="width: 200px; margin-left: -100px;"><div class="message">Processing, please wait...</div><div class="progress progress-striped active"><div class="bar" style="width: 100%;"></div></div></div>');
    UL.loadAnamationSmall = $(document.createElement('div')).addClass('loading-spinner-small');

    UL.Utility = {
        deepCopy: function (objToClone) {
            /// <summary>
            /// creates a deep copy clone of this instance.        
            /// </summary>

            return JSON.parse(JSON.stringify(objToClone));
        },

        createTag: function (tagName) {
            return $(document.createElement(tagName));
        },

        safeFreeze: function (objOrClass) {
            /// <summary>
            /// Freezes an objects structure if browser supports function Object.freeze.
            /// </summary>
            /// <param name="obj"></param>

            if (Object.freeze) {

                Object.freeze(objOrClass);
            }
        },
        mergeObjects: function (orignal, left, right, coflictOverrides) {
            /// <summary>
            /// Merges changes between a baseline object and two changed copies. 
            /// Left win conflicts by default, right wins conflict when a conflict override key matches.
            /// NOTE: This method should not be used on objects with significally different object graphs. 
            /// NOTE2: Arrays will merge over matched index.
            /// </summary>
            /// <param name="orignal" type="Object">The baseline object with no changes. </param>
            /// <param name="left"  type="Object">The left side of the change conflict.</param>
            /// <param name="right" type="Object">The right side of the dchange conflict.</param>
            /// <param name="coflictOverrides" type="Array" optional="true">String[] of key paths to indicate when right side change is used for conflict resolution.
            /// KeyPath Support: "{FieldName/index}/{fieldName/index}" or wildcard "*/{fieldName}"
            /// </param>

            if (window.DeepDiff === undefined) {
                window.console.log("DeepDiff unsuppored in this browser. Right side of merge automatticaly won.");
                return right;
            }

            var orignalClone = UL.Utility.deepCopy(orignal);

            var getOrCreateKeyFunc = function (x) {
                if (x.key === undefined) {
                    x.key = x.path.join("/");
                    x.fieldName = x.path[x.path.length - 1];
                }
                return x.key;
            };

            coflictOverrides = coflictOverrides || [];
            var conflictOverrideDict = Enumerable.From(coflictOverrides).ToDictionary();
            var leftChangeDiff = Enumerable.From(DeepDiff.diff(orignalClone, left));
            var rightChangeDiff = Enumerable.From(DeepDiff.diff(orignalClone, right));
            var conflictKeysDict = leftChangeDiff.Intersect(rightChangeDiff, getOrCreateKeyFunc)
                .ToDictionary(function (x) { return x.key; });

            var resolvedChangeList = [];

            //auto resolve left, left wins by default
            leftChangeDiff.ForEach(function (x) {
                resolvedChangeList.push(x);
            });

            // auto resolve right, right wins when an exclusion exists.
            rightChangeDiff.ForEach(function (x) {
                if (!conflictKeysDict.Contains(x.key) || conflictOverrideDict.Contains(x.key)
                    || conflictOverrideDict.Contains("*/" + x.fieldName)) {
                    resolvedChangeList.push(x);
                }
            });

            //merge changes 
            Enumerable.From(resolvedChangeList).ForEach(function (change) {
                try {
                    DeepDiff.applyChange(orignalClone, true, change);
                }
                catch (e) {
                    window.console.log("Key Skipped during Mapping: " + change.key);
                }
            });

            return orignalClone;

        },
        newGuid: function () {
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
        }
    };

}(jQuery));
