/*
* Scripts for YaleNew Base, Drupal 7
* Revised 8-12
*
*/

// ligature.js v1.0
// http://code.google.com/p/ligature-js/
ligature = function(extended, node) {
  if (!node) {
    ligature(extended, document.body);
  }
  else {
    if (node.nodeType == 3 && node.parentNode.nodeName != 'SCRIPT') {
      node.nodeValue = node.nodeValue
        .replace(/ffl/g, 'ﬄ')
        .replace(/ffi/g, 'ﬃ')
        .replace(/fl/g, 'ﬂ')
        .replace(/fi/g, 'ﬁ')
        .replace(/ff/g, 'ﬀ')

      if (extended) {
        node.nodeValue = node.nodeValue.replace(/ae/g, 'æ')
          .replace(/A[Ee]/g, 'Æ')
          .replace(/oe/g, 'œ')
          .replace(/O[Ee]/g, 'Œ')
          .replace(/ue/g, 'ᵫ')
          .replace(/st/g, 'ﬆ');
      }
    }
    if (node.childNodes) {
      for (var i = 0; i < node.childNodes.length; i++) {
        ligature(extended, node.childNodes.item(i));
      }
    }
  }
};

// Main jQuery scripts
jQuery.noConflict();
  jQuery(document).ready(function($) {
  
    // Mobile Navicon and Canvas Push
    $('#nav-toggle').click(function(e) {
		e.preventDefault();
		$('#region-menu .region-menu-inner').toggleClass('mobile-open');
		$('body').toggleClass('nav-open');
		$('#nav-toggle').toggleClass('nav-ready nav-close');
	});
	
	// AppendAround  
	// Moves the left sidebar menu into the mobile nav based on media queries in
	$('#additional-nav').appendAround();
	
	// Additional Navigation Menu
    $('#additional-nav').click(function(e) {
	    $('#additional-nav').toggleClass('addnav-ready addnav-close');
	    $('.block-menu-block').toggleClass('addnav-open');
	});
	
    // Site title ligature replacement, for IE8 and IE9 only
    if (document.all && !window.atob && document.querySelector) {
      // Load ligatures
      $.each($('h1, h2.site-name'), function(index, element) {
        ligature(false, element);
      });
    }

  // Widon't, http://justinhileman.info/article/a-jquery-widont-snippet/
  // Mod by David Bennett
  $('.site-name, #region-content h2').each(function() {
    $(this).html($(this).html().replace(/\s((?=(([^\s<>]|<[^>]*>)+))\2)\s*$/,'&nbsp;$1'));
  });

  // Fitted, a jQuery Plugin by Trevor Morris
  // http://www.trovster.com/lab/plugins/fitted/
  if(jQuery().fitted) { // initialize only if the Fitted plugin is loaded
    $('.clickable, .flexslider_views_slideshow_slide').fitted();
  }
  
    // Slideshow fade controls
    $('.flex-direction-nav').hide();
    $('.flex-nav-container').hover(function() {
      $('.flex-direction-nav').fadeToggle(200);
    });

}); // End jQuery no-conflict


// FastClick, enables native-like tapping for touch devices
if (window.addEventListener) {
  window.addEventListener('load', function() {
    new FastClick(document.body);
  }, false);
}

