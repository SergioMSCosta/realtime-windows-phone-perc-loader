/*!
* Copyright 2012, Sérgio Costa
* http://www.sergiocosta.me
*
* Please feel free to use, adapt, distribute and do whatever you wish with the code below.
* Please note that parts of this code were not made by me. Some of it was adapted from the example at http://widgets.better2web.com/loader/
* If this code helps you build something cool, a link to my blog (www.sergiocosta.me) somewhere on the credits would be nice, though.
*/

var $slider; // jQuery object for the slider
var $controllableLoader; // jQuery object for the loader

// Sets the progress on the loader
function setProgress(value) {
    $controllableLoader.setValue(value + ' %');
}

// Function called back by the xRTML Repeater tag/controller
// It grabs the message sent by our WP app and processes it
function getData(msg) {
    var value = msg.data.v; // Gets the value
    $("#slider").slider('value', value); // Updates the slider value
    $("#slider").slider('refresh'); // Refreshes the slider
    $controllableLoader.setProgress(value / 100); // Sets the loader progress
    setProgress(value); // Sets the loader value
}

// Sends a message through the Realtime (ORTC) server
function sendValue(val) {
    xRTML.sendMessage('PercentageChannel', val.toString());
}

// Init/constructor
function init() {
    // Sets the controllable progress bar with slider
    $controllableLoader = $("#controllable-loader").percentageLoader({
        width: 233,
        height: 233,
        controllable: true,
        progress: 0,
        onProgressUpdate: function (value) {
            $("#slider").slider('value', Math.round(value * 100.0)); // set the slider value
            $("#slider").slider('refresh'); // refresh the slider
            $controllableLoader.setValue(Math.round(value * 100.0) + ' %'); // update the caption value
            sendValue(Math.round(value * 100.0));
        }
    });

    $slider = $("#slider").slider({
        slide: function (event, ui) {
            $controllableLoader.setProgress(ui.value / 100.0); // update the loader gauge
            $controllableLoader.setValue(ui.value + ' %'); // update the loader caption
            sendValue(ui.value);
        }
    });

    // Realtime stuff
    // Sets the debug to true (so you can see what's happening on the browser console)
    xRTML.Config.debug = true;

    // xRTML Connection
    var connectionJSON = {
        appkey: 'ENTER_YOUR_KEY_HERE', // Your Realtime Application Key (get your free key at www.realtime.co)
        authtoken: 'AUTHENTICATION_TOKEN', // Authorization token. Not necessary if you're using a free developer account. Can be whatever you want (a GUID, for example)
        url: 'http://ortc-developers.realtime.co/server/2.1', // ORTC Server URL
        channels: [{ name: 'PercentageChannel'}] // ORTC Channel(s)
    }
    xRTML.addEventListener('ready', function () {
        xRTML.createConnection(connectionJSON); // Creates the connection when the document is ready
    });

    // Execute Tag
    var executeJSON = {
        name: 'Execute',
        receiveownmessages: false, // Indicates that the tag should ignore its own messages
        callback: getData, // Callback
        triggers: [{ name: "perc"}] // Trigger
    };
    Events.Manager.addEventListener(xRTML, 'ready', function () {
        xRTML.createTag(executeJSON); // Creates the tag when xRTML is ready, connection is made and channels are subscribed
    });
}

$(function () {
    init();
});