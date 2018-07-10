/**
* Fitted: a jQuery Plugin
* @author: Trevor Morris (trovster)
* @url: http://www.trovster.com/lab/code/plugins/jquery.fitted.js
* @documentation: http://www.trovster.com/lab/plugins/fitted/
* @published: 11/09/2008
* @updated: 29/09/2008
* @license Creative Commons Attribution Non-Commercial Share Alike 3.0 Licence
*		   http://creativecommons.org/licenses/by-nc-sa/3.0/
* @notes: 
* Also see BigTarget by Leevi Graham - http://newism.com.au/blog/post/58/bigtarget-js-increasing-the-size-of-clickable-targets/ 
*
*/
if(typeof jQuery != 'undefined') {
	jQuery(function($) {
		$.fn.extend({
			fitted: function(options) {
				var settings = $.extend({}, $.fn.fitted.defaults, options);
							
				return this.each(
					function() {
						
						var $t = $(this);
						var o = $.metadata ? $.extend({}, settings, $t.metadata()) : settings;
						
						if($t.find(':has(a)')) {
							/**
							* Find the first Anchor
							* @var object $a
							*/
							var $a = $t.find('a:first');
							
							/**
							* Get the Anchor Attributes
							*/
							var href = $a.attr('href');
							var title = $a.attr('title');
							
							/**
							* Setup the Container
							* Add the 'container' class defined in settings
							* @event hover
							* @event click
							*/
							$t.addClass(o['class']['container']).hover(
								function(){
									/**
									* Hovered Element
									*/
									$h = $(this);
									
									/**
									* Add the 'hover' class defined in settings
									*/
									$h.addClass(o['class']['hover']);
									
									/**
									* Add the Title Attribute if the option is set, and it's not empty
									*/
									if(typeof o['title'] != 'undefined' && o['title']===true && title != '') {
										$h.attr('title',title);
									}
																		
									/**
									* Set the Status bar string if the option is set
									*/
									if(typeof o['status'] != 'undefined' && o['status']===true) {
										if($.browser.safari) {
											/**
											* Safari Formatted Status bar string
											*/
											window.status = 'Go to "' + href + '"';
										}
										else {
											/**
											* Default Formatted Status bar string
											*/
											window.status = href;
										}
									}
								},
								function(){
									/**
									* "un"-hovered Element
									*/
									$h = $(this);
									
									/**
									* Remove the Title Attribute if it was set by the Plugin
									*/
									if(typeof o['title'] != 'undefined' && o['title']===true && title != '') {
										$h.removeAttr('title');
									}
									
									/**
									* Remove the 'hover' class defined in settings
									*/
									$h.removeClass(o['class']['hover']);
									
									/**
									* Remove the Status bar string
									*/
									window.status = '';
								}
							).click(
								function(){
									/**
									* Clicked!
									* The Container has been Clicked
									* Trigger the Anchor / Follow the Link
									*/
									if($a.is('[rel*=external]')){
										window.open($href);
										return false;
									}
									else {
										//$a.click(); $a.trigger('click');
										window.location = href;
									}
								}
							);
						}
					}
				);
			}
		});
		
		/**
		* Plugin Defaults
		*/
		$.fn.fitted.defaults = {
			'class' : {
				'container' : 'fitted',
				'hover' : 'hovered'
			},
			'title' : true,
			'status' : false
		};
	});
}