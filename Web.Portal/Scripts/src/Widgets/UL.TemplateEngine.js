/// <reference path="../_references.js" />
/// <reference path="../_ULReferences.js" />

(function ($) {
	'use strict';
	if (!window.UL) {
	    window.UL = {};
	}

	UL.TemplateEngine = function (html, options) {
		/// <summary>
	    /// Provides a knock out like databinding for one time template rendering.
	    /// Note: when creating tempates the [=this.someProperty] will be subsituted by the actual value.
	    /// the options object passed in is the object of the "this" context.
		/// </summary>
		/// <param name="html"></param>
		/// <param name="options"></param>
		/// <returns type=""></returns>

		/* this method is give special permission as:
         * 1. To allow insecure regular expressions since they are not being used for validation
         * 2. To allow eval for dynamic function creation, since this funciton is for display purposes only
         */

		/*jslint regexp: true evil: true */
		var re = /\[=([^\]]+)?\]/g,
            reExp = /(^( )?(if|for|else|switch|case|break|\{|\}))(.*)?/g,
            code = 'var r=[];\n',
            cursor = 0;
		var add = function (line, js) {
			if (js) {
				var mResult = line.match(reExp);
				if (mResult) {
					code += line + '\n';
				}
				else {
					code += 'r.push(' + line + ');\n';
				}
			}
			else {
				if (line !== '') {
					code += 'r.push("' + line.replace(/"/g, '\\"') + '");\n';
				}
			}
			return add;
		};

		var match = re.exec(html);
		while (match) {
			add(html.slice(cursor, match.index))(match[1], true);
			cursor = match.index + match[0].length;
			match = re.exec(html);
		}
		add(html.substr(cursor, html.length - cursor));
		code += 'return r.join("");';

		return new Function(code.replace(/[\r\t\n]/g, '')).apply(options);
	};


}(jQuery));