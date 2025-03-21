// returns a layer group for xmap back- and foreground layers
function getXMapBaseLayers(cluster, style, token, attribution) {
	var isEastAsia = cluster.indexOf('cn-n') > -1 || cluster.indexOf('jp-n') > -1;

    var background = L.tileLayer('https://ajaxbg{s}-' + cluster + '.cloud.ptvgroup.com' +
                     '/WMS/GetTile/' + ((style && !isEastAsia) ? 'xmap-' + style + '-bg': 'xmap-ajaxbg') + 
					 '/{x}/{y}/{z}.png' + ((isEastAsia)?  '?xtok=' + token : ''), {
                         minZoom: 0, maxZoom: 19, opacity: 1.0,
                         attribution: attribution,
                         subdomains: '1234'
                     });

	if(isEastAsia)
		return background;
		
    var foreground = new L.NonTiledLayer.WMS('https://ajaxfg-' + cluster + '.cloud.ptvgroup.com/WMS/WMS' + '?xtok=' + token, {
        minZoom: 0, maxZoom: 19, opacity: 1.0,
        layers: style ? 'xmap-' + style + '-fg' : 'xmap-ajaxfg',
        format: 'image/png', transparent: true,
        attribution: attribution
    });

    return L.layerGroup([background, foreground]);
}

// runRequest executes a json request on PTV xServer internet,
// given the url endpoint, the token and the callbacks to be called
// upon completion. The error callback is parameterless, the success
// callback is called with the object returned by the server.
var runRequest = function (url, request, token, handleSuccess, handleError) {
    $.ajax({
        url: url,
        type: "POST",
        data: JSON.stringify(request),

        headers: {
            "Authorization": "Basic " + btoa("xtok:" + token),
            "Content-Type": "application/json"
        },

        success: function (data, status, xhr) {
            handleSuccess(data);
        },

        error: function (xhr, status, error) {
            handleError(xhr);
        }
    });
};
