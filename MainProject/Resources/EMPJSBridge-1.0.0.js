
function alert(msg) {
    var v = "msg|";
    v += msg.toString();
    window.external.notify(v);
}

//alert("init EMPJSBridge.js");

EMPJSBridge = {
    queue: {
        ready: true,
        commands: [],
        timer: null
    },
    _constructors: []
};

// session id for calls
EMPJSBridge.sessionKey = 0;


/**
* List of resource files loaded by EMPJSBridge.
* This is used to ensure JS and other files are loaded only once.
*/
EMPJSBridge.resources = { base: true };

/**
* Determine if resource has been loaded by EMPJSBridge
*
* @param name
* @return
*/
EMPJSBridge.hasResource = function (name) {
    return EMPJSBridge.resources[name];
};

/**
* Add a resource to list of loaded resources by EMPJSBridge
*
* @param name
*/
EMPJSBridge.addResource = function (name) {
    EMPJSBridge.resources[name] = true;
};

/**
* Add an initialization function to a queue that ensures it will run and initialize
* application constructors only once EMPJSBridge has been initialized.
* @param {Function} func The function callback you want run once EMPJSBridge is initialized
*/
EMPJSBridge.addConstructor = function (func) {
    var state = document.readyState;
    if ((state == 'loaded' || state == 'complete')) {
        func();
    }
    else {
        EMPJSBridge._constructors.push(func);
    }
};

(function () {
    var timer = setInterval(function () {

        var state = document.readyState;

        if ((state == 'loaded' || state == 'complete')) {
            clearInterval(timer); // stop looking
            // run our constructors list
            while (setInterval._constructors.length > 0) {
                var constructor = setInterval._constructors.shift();
                try {
                    constructor();
                }
                catch (e) {
                    if (typeof (console['log']) == 'function') {
                        console.log("Failed to run constructor: " + console.processMessage(e));
                    }
                    else {
                        alert("Failed to run constructor: " + e.message);
                    }
                }
            }
            // all constructors run, now fire the deviceready event
            var e = document.createEvent('Events');
            e.initEvent('deviceready');
            document.dispatchEvent(e);
        }
    }, 1);
})();

// centralized callbacks
EMPJSBridge.callbackId = 0;
EMPJSBridge.callbacks = {};
EMPJSBridge.callbackStatus = {
    NO_RESULT: 0,
    OK: 1,
    CLASS_NOT_FOUND_EXCEPTION: 2,
    ILLEGAL_ACCESS_EXCEPTION: 3,
    INSTANTIATION_EXCEPTION: 4,
    MALFORMED_URL_EXCEPTION: 5,
    IO_EXCEPTION: 6,
    INVALID_ACTION: 7,
    JSON_EXCEPTION: 8,
    ERROR: 9
};

/**
* Execute a EMPJSBridge command in a queued fashion, to ensure commands do not
* execute with any race conditions, and only run when EMPJSBridge is ready to
* receive them.
*
*/
EMPJSBridge.exec = function () {
    sendFunctionMessage(arguments);
    //    EMPJSBridge.queue.commands.push(arguments);
    //    if (EMPJSBridge.queue.timer == null)
    //        EMPJSBridge.queue.timer = setInterval(EMPJSBridge.run_command, 10);
};

/**
* Internal function used to dispatch the request to EMPJSBridge.  It processes the
* command queue and executes the next command on the list.  Simple parameters are passed
* as arguments on the url.  JavaScript objects converted into a JSON string and passed as a query string argument of the url.
*
* Arguments may be in one of two formats:
*   FORMAT ONE (preferable)
* The native side will call EMPJSBridge.callbackSuccess or EMPJSBridge.callbackError,
* depending upon the result of the action.
*
* @param {Function} success    The success callback
* @param {Function} fail       The fail callback
* @param {String} service      The name of the service to use
* @param {String} action		The name of the action to use
* @param {String[]} [args]     Zero or more arguments to pass to the method
*
* FORMAT TWO
* @param {String} command Command to be run in EMPJSBridge, e.g. "ClassName.method"
* @param {String[]} [args] Zero or more arguments to pass to the method
* object parameters are passed as an array object [object1, object2] each object will be passed as JSON strings
* @private
*/
EMPJSBridge.run_command = function () {

    if (!EMPJSBridge.queue.ready) {
        return;
    }
    EMPJSBridge.queue.ready = false;

    if (!this.jsBridge) {
        this.jsBridge = document.createElement("iframe");
        this.jsBridge.setAttribute("style", "display:none;");
        this.jsBridge.setAttribute("height", "0px");
        this.jsBridge.setAttribute("width", "0px");
        this.jsBridge.setAttribute("frameborder", "0");
        document.documentElement.appendChild(this.jsBridge);
    }
    var args = EMPJSBridge.queue.commands.shift();
    if (EMPJSBridge.queue.commands.length == 0) {
        clearInterval(EMPJSBridge.queue.timer);
        EMPJSBridge.queue.timer = null;
    }

    var service;
    var callbackId = null;
    var start = 0;
    try {
        if (args[0] == null || typeof args[0] === "function") {
            var success = args[0];
            var fail = args[1];
            service = args[2] + "." + args[3];
            args = args[4];  //array of arguments to
            callbackId = service + EMPJSBridge.callbackId++;
            if (success || fail) {
                EMPJSBridge.callbacks[callbackId] = { success: success, fail: fail };
            }
        } else {
            service = args[0];
            start = 1;
        }

        var uri = [];
        var dict = null;

        if (args != null) {
            for (var i = start; i < args.length; i++) {
                var arg = args[i];
                if (arg == undefined || arg == null)
                    continue;
                if (typeof (arg) == 'object') {
                    dict = arg;
                }
                else {
                    uri.push(encodeURIComponent(arg));
                }
            }
        }

        var next = callbackId != null ? ("/" + callbackId + "/") : "/";
        //add the sessionId in the user field of the URL conforming to RFC1808
        //emp://-1134476704@ryt.sms.send/ryt.sms.send0/1123123/hello
        var url = "emp://" + EMPJSBridge.sessionKey + "@" + service + next + uri.join("/");

        if (dict != null) {
            url += "?" + encodeURIComponent(JSON.stringify(dict));
        }
        this.jsBridge.src = url;
    } catch (e) {
        alert(e);
        console.log("EMPJSBridgeExec Error: " + e);
    }
};
/**
* Called by native code when returning successful result from an action.
*
* @param callbackId
* @param args
*		args.status - EMPJSBridge.callbackStatus
*		args.message - return value
*		args.keepCallback - 0 to remove callback, 1 to keep callback in EMPJSBridge.callbacks[]
*/
EMPJSBridge.callbackSuccess = function (callbackId, args) {
    alert("callbacksuccess");
    if (EMPJSBridge.callbacks[callbackId]) {

        // If result is to be sent to callback
        if (args.status == EMPJSBridge.callbackStatus.OK) {
            try {
                if (EMPJSBridge.callbacks[callbackId].success) {
                    EMPJSBridge.callbacks[callbackId].success(args.message);
                }
            }
            catch (e) {
                console.log("Error in success callback: " + callbackId + " = " + e);
            }
        }

        // Clear callback if not expecting any more results
        if (!args.keepCallback) {
            delete EMPJSBridge.callbacks[callbackId];
        }
    }
};

/**
* Called by native code when returning error result from an action.
*
* @param callbackId
* @param args
*/
EMPJSBridge.callbackError = function (callbackId, args) {
    if (EMPJSBridge.callbacks[callbackId]) {
        try {
            if (EMPJSBridge.callbacks[callbackId].fail) {
                EMPJSBridge.callbacks[callbackId].fail(args.message);
            }
        }
        catch (e) {
            console.log("Error in error callback: " + callbackId + " = " + e);
        }

        // Clear callback if not expecting any more results
        if (!args.keepCallback) {
            delete EMPJSBridge.callbacks[callbackId];
        }
    }
};


/**
* Does a deep clone of the object.
*
* @param obj
* @return
*/
EMPJSBridge.clone = function (obj) {
    if (!obj) {
        return obj;
    }

    if (obj instanceof Array) {
        var retVal = new Array();
        for (var i = 0; i < obj.length; ++i) {
            retVal.push(EMPJSBridge.clone(obj[i]));
        }
        return retVal;
    }

    if (obj instanceof Function) {
        return obj;
    }

    if (!(obj instanceof Object)) {
        return obj;
    }

    if (obj instanceof Date) {
        return obj;
    }

    retVal = new Object();
    for (i in obj) {
        if (!(i in retVal) || retVal[i] != obj[i]) {
            retVal[i] = EMPJSBridge.clone(obj[i]);
        }
    }
    return retVal;
};

// Intercept calls to document.addEventListener and watch for unload

EMPJSBridge.m_document_addEventListener = document.addEventListener;

document.addEventListener = function (evt, handler, capture) {
    var e = evt.toLowerCase();
    if (e === 'unload') {
        EMPJSBridge.onUnload = function (e) { return handler(e); };
    }
    else {
        EMPJSBridge.m_document_addEventListener.call(document, evt, handler, capture);
    }
};

// Intercept calls to document.removeEventListener and watch for events that
// are generated by EMPJSBridge native code

EMPJSBridge.m_document_removeEventListener = document.removeEventListener;

document.removeEventListener = function (evt, handler, capture) {
    var e = evt.toLowerCase();

    if (e === 'unload') {
        EMPJSBridge.onUnload = null;
    }

    EMPJSBridge.m_document_removeEventListener.call(document, evt, handler, capture);
};

/**
* Method to fire event from native code
*/
EMPJSBridge.fireEvent = function (type, target) {
    var e = document.createEvent('Events');
    e.initEvent(type);

    target = target || document;
    if (target.dispatchEvent === undefined) { // ie window.dispatchEvent is undefined in iOS 3.x
        target = document;
    }

    target.dispatchEvent(e);
};

String.prototype.compare = function (str) {
    //²»Çø·Ö´óÐ¡Ð´
    if (this.toLowerCase() == str.toLowerCase()) {
        return "1"; // ÕýÈ·
    }
    else {
        return "0"; // ´íÎó
    }
}

EMPCallBackHelper = function (invokeMethod, errorCode, params) {
    var code = 0;
    if (errorCode != "0") {
        code = -1;
    }

    if (typeof params == 'undefined') {
        this[invokeMethod].call("tempString", code);
    }
    else if (typeof params == 'string') {
        if (params.compare("True") == 1) {
            this[invokeMethod].call("tempString", code, true);
        }
        else if (params.compare("False") == 1) {
            this[invokeMethod].call("tempString", code, false);
        }
        else {
            var fValue = parseFloat(params);
            if (isNaN(fValue)) {
                this[invokeMethod].call("tempString", code, params);
            }
            else {
                this[invokeMethod].call("tempString", code, fValue);
            }
        }
}
else {
        alert("EMPCallBackHelper else");
}
};

window.back = function () {
    EMPJSBridge.exec(null, "ryt.window", "back", null);
};

/**
********************* command change ********************
*/
function getFuncName(_callee) {
    var _text = _callee.toString();
    var _scriptArr = document.scripts;
    for (var i = 0; i < _scriptArr.length; i++) {
        var _start = _scriptArr[i].text.indexOf(_text);
        if (_start != -1) {
            if (/^function\s*\(.*\).*\r\n/.test(_text)) {
                var _tempArr = _scriptArr[i].text.substr(0, _start).split('\r\n');
                return _tempArr[_tempArr.length - 1].replace(/(var)|(\s*)/g, '').replace(/=/g, '');
            } else {
                return  _text.match(/^function\s*([^\(]+).*[\(]/)[1];
            }
        }
    }
}


function sendFunctionMessage(arguments) {

    var paramss = arguments[1] + "|" + arguments[2];
    if (arguments[0] != null && (typeof arguments[0] == "function")) {
        paramss += "|" + getFuncName(arguments[0]);
    }
    if (arguments[3] != null) {
        for (var i = 0; i < arguments[3].length; i++) {
            paramss += "|" + arguments[3][i];
        }

    }
    window.external.notify(paramss);
}

/**
********************* Camera ********************
*/
if (!EMPJSBridge.hasResource("camera")) {
    EMPJSBridge.addResource("camera");


    camera = function () {
    }

    /**
    * Gets a picture from source defined by "options.sourceType", and returns the
    * image as defined by the "options.destinationType" option.
     
    * The defaults are sourceType=CAMERA and destinationType=DATA_URL.
    *
    * @param {Function} successCallback
    * @param {Function} errorCallback
    * @param {Object} options
    */
    camera.open = function (callback) {
        EMPJSBridge.exec(callback, "ryt.camera", "open", null);
    };

    EMPJSBridge.addConstructor(
                               function () {
                                   if (typeof camera == "undefined") {
                                       //camera = new camera();
                                   }
                               }
                               );
};


/**
********************* SMS ********************
*/
if (!EMPJSBridge.hasResource("sms")) {
    EMPJSBridge.addResource("sms");

    sms = function () {

    }

    sms.send = function (phoneNum, content, callback) {
        try {
            var options = [];
            options.push(phoneNum);
            options.push(content);
            EMPJSBridge.exec(callback, "ryt.sms", "send", options);
        } catch (e) {
            alert(e);
        }

    };

    EMPJSBridge.addConstructor(
                               function () {
                                   if (typeof sms == "undefined");
                               }
                               );
};

Position = function () {
    this.latitude = null;
    this.longitude = null;
}

// ÎªÁË´«Position¶ÔÏó£¬³é³öÀ´Ò»¸ö·½·¨£ºGetCurrentLocationCallbackHelper
GetCurrentLocationCallbackHelper = function (errorCode, lati, lon) {
    var info = { latitude: lati, longitude: lon };

    var code = 0;
    if (errorCode != "0") { code = -1 }
    gpsCallback(code, info);
}

/**
********************* Geolocation ********************
*/
if (!EMPJSBridge.hasResource("geolocation")) {
    EMPJSBridge.addResource("geolocation");

    geolocation = function () {

    }

    /**
    * get gps location
    */
    var gpsCallback;
    geolocation.getCurrentLocation = function (callback, accuracy) {
        try {
            var options = [];
            options.push("GetCurrentLocationCallbackHelper");
            options.push(accuracy);
            gpsCallback = callback;
            EMPJSBridge.exec(null, "ryt.geolocation", "getCurrentLocation", options);
        } catch (e) {
            alert(e);
        }
    };

    EMPJSBridge.addConstructor(
                               function () {
                                   if (typeof geolocation == "undefined");
                               }
                               );
};

Acceleration = function (x, y, z) {
    this._x = x;
    this._y = y;
    this._z = z;
}

// ÎªÁË´«Acceleration¶ÔÏó£¬³é³öÀ´Ò»¸ö·½·¨£ºGetCurrentLocationCallbackHelper
GetAccelerationCallbackHelper = function (errorCode, _x, _y, _z) {
    var info = { x: _x, y: _y, z: _z };

    var code = 0;
    if (errorCode != "0") {
        code = -1;
    }

    accelerometerCallback(code, info);
}

/**
********************* Accelerometer ********************
*/
if (!EMPJSBridge.hasResource("accelerometer")) {
    EMPJSBridge.addResource("accelerometer");

    accelerometer = function () {
    }

    var accelerometerCallback;
    accelerometer.startAccelerometer = function (callback, interval) {
        try {
            accelerometerCallback = callback;
            var options = [];
            options.push("GetAccelerationCallbackHelper");
            options.push(interval);
            EMPJSBridge.exec(null, "ryt.accelerometer", "startAccelerometer", options);
        } catch (e) {
            alert(e);
        }
    };
    accelerometer.stopAccelerometer = function (callback) {
        try {
            EMPJSBridge.exec(callback, "ryt.accelerometer", "stopAccelerometer", null);
        } catch (e) {
            alert(e);
        }
    };

    EMPJSBridge.addConstructor(
                               function () {
                                   if (typeof accelerometer == "undefined");
                               }
                               );
};

var Person = function (firstName, middleName, lastName, phoneNumber, email, address) {
    this._firstName = firstName;
    this._middleName = middleName;
    this._lastName = lastName;
    this._phoneNumber = phoneNumber;
    this._email = email;
    this._address = address;
}

var contractOpenCallback = function (errorCode, array) { };

var ContractOpenHelper = function (errorCode, dName, phoneNum) {
    var info = { firstName: dName, phoneNumber: phoneNum };

    var code = 0;
    if (errorCode != "0") {
        code = -1;
    }
    contractOpenCallback(code, info);
}

/**
********************* Contact ********************
*/
if (!EMPJSBridge.hasResource("contact")) {
    EMPJSBridge.addResource("contact");

    contact = function () {
    }

    contact.open = function (callback) {
        try {
            contractOpenCallback = callback;
            var options = [];
            options.push("ContractOpenHelper");
            EMPJSBridge.exec(null, "ryt.contact", "open", options);
        } catch (e) {
            alert(e);
        }
    };

    contact.add = function (array, callback) {
        try {

            var options = [];
            options.push(array[0].firstName);
            options.push(array[0].lastName);
            options.push(array[0].email);
            options.push(array[0].address);
            options.push(array[0].phoneNumber);

            EMPJSBridge.exec(callback, "ryt.contact", "add", options);

        } catch (e) {
            alert(e);
        }
    };

    contact.copy = function (callback) {
        try {
            EMPJSBridge.exec(callback, "ryt.contact", "copy", null);
        } catch (e) {
            alert(e);
        }
    };

    EMPJSBridge.addConstructor(
                               function () {
                                   if (typeof contact == "undefined");
                               }
                               );
};



/**
********************* Device ********************
*/
if (!EMPJSBridge.hasResource("device")) {
    EMPJSBridge.addResource("device");

    var Device = function () {
        var options = [];
        options.push("deviceSuccessCallback");
        EMPJSBridge.exec(null, "ryt.device", "getDeviceInfo", options);
    };

    function deviceSuccessCallback(platform, version, name, uuid) {
        Device.prototype.available = true;
        Device.prototype.platform = platform;
        Device.prototype.version = version;
        Device.prototype.name = name;
        Device.prototype.uuid = uuid;
    }

    if (typeof device == "undefined") {
        device = new Device();
    }
};

/**
********************* HTTP ********************
*/
if (!EMPJSBridge.hasResource("http")) {
    EMPJSBridge.addResource("http");

    var Http = function () {

        var options = [];
        options.push("httpTypeCallback");
        EMPJSBridge.exec(null, "ryt.http", "connectType", options);
    }

    function httpTypeCallback(value) {
        Http.prototype.connectType = value;
    }

    Http.prototype.isReachable = function (uri, callback) {
        try {
            var options = [];
            options.push(uri);
            EMPJSBridge.exec(callback, "ryt.http", "isReachable", options);
        } catch (e) {
            alert(e);
        }
    };

    if (typeof http == "undefined") {
        http = new Http();
    };
};

/**
********************* FILE ********************
*/
if (!EMPJSBridge.hasResource("file")) {
    EMPJSBridge.addResource("file");

    file = function () {
    }

    /**
    * get file location
    *
    *
    * @param {string} phoneNum
    * @param {string} content
    * @param {Function} callback
    */
    file.write = function (name, data, callback) {
        try {
            var options = [];
            options.push(name);
            options.push(data);
            EMPJSBridge.exec(callback, "ryt.file", "write", options);
        } catch (e) {
            alert(e);
        }
    };

    file.read = function (fileName, fileType, callback) {
        try {
            var options = [];
            options.push(fileName);
            options.push(fileType);
            EMPJSBridge.exec(callback, "ryt.file", "read", options);
        } catch (e) {
            alert(e);
        }
    };

    file.remove = function (fileName, callback) {
        try {
            var options = [];
            options.push(fileName);
            EMPJSBridge.exec(callback, "ryt.file", "remove", options);
        } catch (e) {
            alert(e);
        }
    };

    file.isExist = function (fileName, callback) {
        try {
            var options = [];
            options.push(fileName);
            EMPJSBridge.exec(callback, "ryt.file", "isExist", options);
        } catch (e) {
            alert(e);
        }
    };

    EMPJSBridge.addConstructor(
                               function () {
                                   if (typeof file == "undefined");
                               }
                               );
};

/**
********************* VIDEO ********************
*/

function UUIDcreatePart(length) {
    var uuidpart = "";
    for (var i = 0; i < length; i++) {
        var uuidchar = parseInt((Math.random() * 256), 10).toString(16);
        if (uuidchar.length == 1) {
            uuidchar = "0" + uuidchar;
        }
        uuidpart += uuidchar;
    }
    return uuidpart;
}

utils = function () {
}

utils.createUUID = function () {
    return UUIDcreatePart(4) + '-' +
    UUIDcreatePart(2) + '-' +
    UUIDcreatePart(2) + '-' +
    UUIDcreatePart(2) + '-' +
    UUIDcreatePart(6);
};

var mediaObjects = {};

if (!EMPJSBridge.hasResource("video")) {
    EMPJSBridge.addResource("video");

    var Video = function (src, callback) {
        this.id = utils.createUUID();
        mediaObjects[this.id] = this;

        this.src = src;

        EMPJSBridge.exec(callback, "ryt.video", "load", [this.src, this.id]);

        //        if (videoFrame == null) {
        //            EMPJSBridge.exec(this.callback, "ryt.video", "load", [this.src, this.id]);
        //        }
        //        else {
        //            EMPJSBridge.exec(this.callback, "ryt.video", "load", [this.src, this.id, videoFrame.x, videoFrame.y, videoFrame.width, videoFrame.height]);
        //        }
    }

    Video.prototype.dispose = function () {
        try {
            EMPJSBridge.exec(null, "ryt.video", "dispose", [this.id]);
        } catch (e) {
            alert(e);
        }
    };

    Video.prototype.play = function (videoFrame) {
        try {

            if (videoFrame == null) {
                EMPJSBridge.exec(null, "ryt.video", "play", [this.id]);
            }
            else {
                EMPJSBridge.exec(null, "ryt.video", "play", [this.id, videoFrame.x, videoFrame.y, videoFrame.width, videoFrame.height]);
            }

        } catch (e) {
            alert(e);
        }
    };

    Video.prototype.stop = function () {
        EMPJSBridge.exec(null, "ryt.video", "stop", [this.id]);
    };

    Video.prototype.pause = function () {
        EMPJSBridge.exec(null, "ryt.video", "pause", [this.id]);
    };

    Video.prototype.resume = function () {
        try {
            EMPJSBridge.exec(null, "ryt.video", "resume", [this.id]);
        } catch (e) {
            alert(e);
        }
    };
};

/**
********************* AUDIO ********************
*/
if (!EMPJSBridge.hasResource("audio")) {
    EMPJSBridge.addResource("audio");

    var Audio = function (src, callback) {
        this.id = utils.createUUID();
        mediaObjects[this.id] = this;
        this.src = src;
        this.callback = callback;

        EMPJSBridge.exec(callback, "ryt.audio", "load", [this.src, this.id]);
    }

    Audio.prototype.dispose = function () {
        try {
            EMPJSBridge.exec(null, "ryt.audio", "dispose", [this.id]);
        } catch (e) {
            alert(e);
        }
    };

    /**
    * Start or resume playing audio file.
    */
    Audio.prototype.play = function (numberOfLoops) {
        try {
            EMPJSBridge.exec(null, "ryt.audio", "play", [this.id, numberOfLoops]);

        } catch (e) {
            alert(e);
        }
    };

    /**
    * Stop playing audio file.
    */
    Audio.prototype.stop = function () {
        try {
            EMPJSBridge.exec(null, "ryt.audio", "stop", [this.id]);

        } catch (e) {
            alert(e);
        }
    };

    /**
    * Pause playing audio file.
    */
    Audio.prototype.pause = function () {
        try {
            EMPJSBridge.exec(null, "ryt.audio", "pause", [this.id]);
        } catch (e) {
            alert(e);
        }
    };


    Audio.prototype.resume = function () {
        try {
            EMPJSBridge.exec(null, "ryt.audio", "resume", [this.id]);
        } catch (e) {
            alert(e);
        }
    };

    Audio.prototype.getMaxVolume = function (callback) {
        try {
            EMPJSBridge.exec(callback, "ryt.audio", "getMaxVolume", [this.id]);
        } catch (e) {
            alert(e);
        }
    };
    Audio.prototype.getMinVolume = function (callback) {
        try {
            EMPJSBridge.exec(callback, "ryt.audio", "getMinVolume", [this.id]);
        } catch (e) {
            alert(e);
        }
    };
    Audio.prototype.getVolume = function (callback) {
        try {
            EMPJSBridge.exec(callback, "ryt.audio", "getVolume", [this.id]);
        } catch (e) {
            alert(e);
        }
    };
    Audio.prototype.setVolume = function (volume, callback) {
        try {
            EMPJSBridge.exec(callback, "ryt.audio", "setVolume", [this.id, volume]);
        } catch (e) {
            alert(e);
        }
    };
};

/**
********************* database ********************
*/

if (!EMPJSBridge.hasResource("databse")) {
    EMPJSBridge.addResource("databse");

    database = function () {
    };

    //    database.open = function (dbName, callBackMethod) {
    //        try {

    //            database.prototype.name = dbName;
    //            database.prototype.sql = null;
    //            dbObjects[dbName] = this;

    //            var options = [];
    //            options.push(dbName);
    //            EMPJSBridge.exec(callBackMethod, "ryt.database", "open", options);
    //        } catch (e) {
    //            alert(e);
    //        }
    //    };

    //    database.exec = function (sql) {
    //        try {
    //            EMPJSBridge.exec(callback, "ryt.database", "exec", [this.name, sql]);
    //        } catch (e) {
    //            alert(e);
    //        }
    //    };

    //    database.close = function () {
    //        try {
    //            EMPJSBridge.exec(callback, "ryt.database", "close", [this.name]);
    //        } catch (e) {
    //            alert(e);
    //        }
    //    };

    database.addData = function (key, value, callback) {
        try {
            var options = [];
            options.push(key);
            options.push(value);

            EMPJSBridge.exec(callback, "ryt.database", "addData", options);
        } catch (e) {
            alert(e);
        }
    };

    database.getData = function (key, callback) {
        try {
            var options = [];
            options.push(key);
            EMPJSBridge.exec(callback, "ryt.database", "getData", options);
        } catch (e) {
            alert(e);
        }
    };

    database.insertData = function (key, value, callback) {
        try {
            var options = [];
            options.push(key);
            options.push(value);
            EMPJSBridge.exec(callback, "ryt.database", "insertData", options);
        } catch (e) {
            alert(e);
        }
    };

    database.updateData = function (key, value, callback) {
        try {
            var options = [];
            options.push(key);
            options.push(value);
            EMPJSBridge.exec(callback, "ryt.database", "updateData", options);
        } catch (e) {
            alert(e);
        }
    };
};

//=====================================================================

// DB Exec Helpe
DB_ExecHelper = function (exeCode) {
    if (execBack == null) {
        return;
    }
    var len = arguments.length;
    alert(len);
    if (len == 2) {
        var array1 = arguments[1].split("{+(IOL}");
        execBack(exeCode, array1);
    }
    else if (len == 1) {
        execBack(exeCode);
    }
}

var dbObjects = {};
if (!EMPJSBridge.hasResource("Database")) {
    EMPJSBridge.addResource("Database");

    Database = function (name, callback) {
        this.name = name;

        this.sql = null;
        this.id = utils.createUUID();
        dbObjects[this.id] = this;

        EMPJSBridge.exec(callback, "ryt.database", "open", [this.name]);
    }

    Database.prototype.close = function (callback) {
        try {
            EMPJSBridge.exec(callback, "ryt.database", "close", [this.name]);
        } catch (e) {
            alert(e);
        }
    };

    var execBack;
    Database.prototype.exec = function (sql, callBack) {
        try {
            execBack = callBack;
            this.sql = sql;
            EMPJSBridge.exec(null, "ryt.database", "exec", ["DB_ExecHelper", this.sql, this.name]);
        } catch (e) {
            alert(e);
        }
    }
};



