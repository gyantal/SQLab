

// for Dates, JS only understand numbers, so it cannot Parse '2015-11-16T...' to Date. So, it keeps it as string, so I keep it as String here,
// var jsonObject = JSON.parse('{"date":1251877601000}');
// new Date(1293034567877);
// but I have a Date ojbect under it in which I manually set it up properly
export interface HMData {
    AppOk: string;
    StartDate: string;        // JS jannot JSON.Parse proper string dates
    StartDateLoc: Date;
    StartDateTimeSpanStr: string;
    DailyEmailReportEnabled: boolean;

    RtpsOk: string;
    RtpsTimerEnabled: boolean;
    RtpsTimerFrequencyMinutes: number;
    RtpsDownloads: string[];

    VBrokerOk: string;
    ProcessingVBrokerMessagesEnabled: boolean;
    VBrokerReports: string[];
    VBrokerDetailedReports: string[];

    CommandToBackEnd: string;       // "OnlyGetData", "ApplyTheDifferences"
    ResponseToFrontEnd: string;     // it is "OK" or the Error message
}