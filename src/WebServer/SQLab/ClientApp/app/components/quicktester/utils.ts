

function getTimestampMilliseconds(): number {
    return (new Date()).getTime();
}

//https://github.com/electricessence/TypeScript.NET/blob/master/source/System/Diagnostics/Stopwatch.ts
export class StopWatch {

    static getTimestampMilliseconds(): number {
        return getTimestampMilliseconds();
    }

    private _elapsed: number = 0;
    private _startTimeStamp: number = 0;

    private _isRunning: boolean = false;

    constructor() {
        this.reset();
    }

    static startNew(): StopWatch {
        var s = new StopWatch();
        s.start();
        return s;
    }

    start(): void {
        const _ = this;
        if (!_._isRunning) {
            _._startTimeStamp = getTimestampMilliseconds();
            _._isRunning = true;
        }
    }

    reset(): void {
        const _ = this;
        _._elapsed = 0;
        _._isRunning = false;
        _._startTimeStamp = NaN;
    }

    GetTimestampInMsec(): number {
        var elapsedInMsec: number = new Date().getTime() - this._startTimeStamp;
        return elapsedInMsec;
    }

}