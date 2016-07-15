/// <reference path="../_references.js" />

//This file is to contain prototype extensions to the native js objects.

(function () {

    "use strict";


    window.GetFunctionRef = function (functionName) {
    	/// <summary>
        /// Gets a referneces to the function by its specified name; 
        /// this function supports global functions, namespaces supported.
    	/// </summary>
    	/// <param name="functionName"></param>
        
        var segments = functionName.split(".");
        var i = 0;
        var ref = window;
        var tempRef = null;
        for (i = 0; i < segments.length; i++) {
            tempRef = ref[segments[i]];
            if (tempRef === undefined || tempRef === null) {
                return null;
            }

            ref = tempRef;
        }

        return ref;
    };  

    Array.prototype.find = function (lamdaFunc) {
        /// <summary>
        /// Finds items that are identified by the expression.
        /// </summary>
        /// <param name="lamdaFunc" type="BooleanFunction">the expression function</param>
        /// <returns type=""></returns>
        var i = 0;
        var internalList = this;
        for (i = 0; i < internalList.length; i++) {
            if (lamdaFunc(internalList[i])) {
                return internalList[i];
            }
        }
    };

    Array.prototype.where = function (lamdaFunc) {
        /// <summary>
        /// Provides a lambda where query
        /// </summary>
        /// <param name="lamdaFunc" type="Bool Function(Objec x)">The lambda expression expressed in js function format.</param>
        /// <returns type="Array<T>">The matching results array.</returns>
        var tempArray = [];
        var internalList = this;
        var i = 0;

        for (i = 0; i < internalList.length; i++) {
            if (lamdaFunc(internalList[i])) {
                tempArray.push(internalList[i]);
            }
        }

        return tempArray;
    };

    Array.prototype.insert = function (index, item) {
    	/// <summary>
    	/// Inserts the specified item into the array at the secified index.
    	/// </summary>
    	/// <param name="index">The Position to insert the item in the array</param>
    	/// <param name="item">The item to be inserted</param>
        this.splice(index, 0, item);
    };

    Array.prototype.remove = function (lamdaFuncOrItemObject) {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="lamdaFuncOrItemObject"></param>

        var internalList = this;
        var i = 0;
        var paramType = typeof lamdaFuncOrItemObject;

        for (i = 0; i < internalList.length; i++) {
            if (paramType === 'function' || paramType === 'Function') {
                if (lamdaFuncOrItemObject(internalList[i])) {
                    internalList.splice(i, 1);
                }
            }
            else {
                if (internalList[i] === lamdaFuncOrItemObject) {
                    internalList.splice(i, 1);
                }
            }
        }
    };

    if (!Array.prototype.indexOf) {
        Array.prototype.indexOf = function (what, i) {
            /// <summary>
            /// 
            /// </summary>
            /// <param name="what"></param>
            /// <param name="i"></param>
            /// <returns type=""></returns>
            i = i || 0;
            var L = this.length;
            while (i < L) {
                if (this[i] === what) {
                    return i;
                }
                ++i;
            }
            return -1;
        };
    }

    // For IE8 and earlier version. 
    if (!Date.now) {
        Date.now = function () {
            return new Date().valueOf();
        };
    }

    if (!String.prototype.contains) {
        String.prototype.contains = function (searchStr) {
        	/// <summary>
        	/// Indicates whether the specified string is contained in this instance of string.
        	/// </summary>
        	/// <param name="searchStr">The string to identifiy in this instance.</param>
            /// <returns type="">True if found, otherwiwse false.</returns>

            return (this.indexOf(searchStr) > 0);
        };
    }


    String.prototype.count = function (regex) {
    	/// <summary>
        /// Returns a count for the number of 
        /// matches to the specified regular expression
    	/// </summary>
    	/// <param name="regex">The regular express to use for matching</param>
    	/// <returns type="Number">The match count.</returns>
        var result = this.match(regex);
        if (result && result.length) {
            return result.length;
        }

        return 0;
    };

    if (!String.prototype.trim) {
        String.prototype.trim = function () {
        	/// <summary>
            /// Trims white space from begining and end of the string.
            /// this is a polyfill for ie8
        	/// </summary>
        	/// <returns type=""></returns>
            return this.replace(/^\s+|\s+$/g, '');
        };
    }

    if (!String.format) {
        String.format = function (format, args) {
            /// <summary>
            /// Replaces replaces tokens pased on argument position using c# notation. 
            /// Currenlty Doesn't support value formating, only replacement.
            /// </summary>
            /// <param name="format">The format string using c# notation</param>
            /// <param name="args"></param>
            var i;
            if (args instanceof Array) {
                for (i = 0; i < args.length; i++) {
                    format = format.replace(new RegExp('\\{' + i + '\\}', 'gm'), args[i]);
                }
                return format;
            }
           
            for (i = 1; i < arguments.length; i++) {
                format = format.replace(new RegExp('\\{' + i + '\\}', 'gm'), arguments[i]);
            }
            return format;
        };
    }


    String.formatTokens = function (format, tokenContainer) {
        /// <summary>
        /// Replaces named Tokens, from tokenContainer object.
        /// </summary>
        /// <param name="format">Tokenized format string.</param>
        /// <param name="tokenContainer">Object Containing members whose names were used as tokens.</param>
        var token = null;
        for (token in tokenContainer) {
            if (tokenContainer.hasOwnProperty(token)) {
                format = format.replace(new RegExp('\\{' + token + '\\}', 'gm'), tokenContainer[token]);
            }
        }
        return format;
    };
    if (!String.prototype.htmlEncode) {
        String.prototype.htmlEncode = function() {
            /// <summary>
            /// HTML encodes a string value 
            /// </summary>
            var value = this;
            return $("<div/>").text(value).html();
        };
    }
    if (!String.prototype.htmlDecode) {
        String.prototype.htmlDecode = function () {
            /// <summary>
            /// Html decodes a string value
            /// </summary>
            var value = this;
            return $("<div/>").html(value).text();

        };
    }

    if (String.isString === undefined) {
        /// <summary>
        /// checks if target is a string.
        /// </summary>

        String.isString = function (target) {
            return (Object.prototype.toString.call(target) === '[object String]');
        };
    }

}());

