

(function ($) {
    'use strict';
    if (!window.UL) {
        window.UL = {};
    }

    UL.Tabs = function (element, options) {
    	/// <summary>
    	///  
    	/// </summary>
    	/// <param name="element"></param>
    	/// <param name="options"></param>
    	/// <returns type=""></returns>
        this.tabHeadContainer = element;
        this.selectedTab = null;
        this.defaultTab = null;
        this.tabPanelClass = '.modal-tab-panel';
        if (options) {
            this.selectedTab = options.selectedTab;
            this.defaultTab = options.defaultTab;
        }
        this.init();
        return this;
    };

    UL.Tabs.prototype = {
        init: function () {
            var self = this;

           // self.tabHeadContainer = element;
            $(self.tabPanelClass).hide();
            self.tabHeadContainer.find('a.accordion-toggle').click(function (e) {
                e.preventDefault();
                var key = $(this).prop("id");
                self.changeTab(key);
            });

            if (self.selectedTab) {
                var tabExists = $("#" + self.selectedTab).length > 0;
                if (tabExists) {
                    self.defaultTab = self.selectedTab;
                }
            }
            self.changeTab(self.defaultTab);
            return self;
        },
        changeTab: function (tabKey) {
            var self = this;
            self.tabHeadContainer.find('li.accordion-heading').removeClass('active');
            var link = $("#" + tabKey);
            link.parent('.accordion-heading').addClass('active');
            $(self.tabPanelClass).hide();
            $(link.data("tabSelector")).show();
            return self;
        }
    };


    $.fn.tabfy = function(options) {
        this.control = new UL.Tabs($(this), options);
        return $(this);
    };

}(jQuery));

