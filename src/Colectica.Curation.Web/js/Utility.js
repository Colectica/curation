function endsWith(str, suffix) {
    return str.indexOf(suffix, str.length - suffix.length) !== -1;
}

function getFileType(fileName) {

    var lower = fileName.toLowerCase();

    if (endsWith(lower, ".dta") ||
        endsWith(lower, ".sav") ||
        endsWith(lower, ".rdata") ||
        endsWith(lower, ".csv")) {
        return "Data";
    }

    if (endsWith(lower, ".do") ||
        endsWith(lower, ".r") ||
        endsWith(lower, ".sps")) {
        return "Program";
    }

    return "Supplementary Materials";
}

function getFileExtension(fileName) {
    return fileName.split('.').pop();
}



function bytesToSize(bytes, precision) {
    var kilobyte = 1024;
    var megabyte = kilobyte * 1024;
    var gigabyte = megabyte * 1024;
    var terabyte = gigabyte * 1024;

    if ((bytes >= 0) && (bytes < kilobyte)) {
        return bytes + ' B';

    } else if ((bytes >= kilobyte) && (bytes < megabyte)) {
        return (bytes / kilobyte).toFixed(precision) + ' KB';

    } else if ((bytes >= megabyte) && (bytes < gigabyte)) {
        return (bytes / megabyte).toFixed(precision) + ' MB';

    } else if ((bytes >= gigabyte) && (bytes < terabyte)) {
        return (bytes / gigabyte).toFixed(precision) + ' GB';

    } else if (bytes >= terabyte) {
        return (bytes / terabyte).toFixed(precision) + ' TB';

    } else {
        return bytes + ' B';
    }
}
