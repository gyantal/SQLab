function lbgaussian(mean, stdev) {
    // returns a gaussian random function with the given mean and stdev.
    var y2;
    var use_last = false;
    return function () {
        var y1;
        if (use_last) {
            y1 = y2;
            use_last = false;
        }
        else {
            var x1, x2, w;
            do {
                x1 = 2.0 * Math.random() - 1.0;
                x2 = 2.0 * Math.random() - 1.0;
                w = x1 * x1 + x2 * x2;
            } while (w >= 1.0);
            w = Math.sqrt((-2.0 * Math.log(w)) / w);
            y1 = x1 * w;
            y2 = x2 * w;
            use_last = true;
        }

        var retval = mean + stdev * y1;
        return retval;

    }
}

function lbaverage(arrayB) {
    var total = 0;
    for (var i = 0; i < arrayB.length; i++) {
        total += arrayB[i];
    }
    var avg = total / arrayB.length;
    return avg;
}

function lbmedian(arrayB) {
    arrayB.sort(function (a, b) {
        return a - b;
    });
    var i = arrayB.length / 2;
    i % 1 === 0 ? med = (arrayB[Math.floor(i) - 1] + arrayB[Math.floor(i)]) / 2 : med = arrayB[Math.floor(i)];
    return med;
}
function lbstdDev(arrayB) {
    var avg = lbaverage(arrayB);
    var sumdev = 0;
    for (var i = 0; i < arrayB.length; i++) {
        sumdev += (arrayB[i]-avg)*(arrayB[i]-avg);
    }
    var stdDev = Math.sqrt(sumdev/(arrayB.length-1));
    return stdDev;
}