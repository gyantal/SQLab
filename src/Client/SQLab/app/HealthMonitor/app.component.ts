import {Component} from '@angular/core';

@Component({
    selector: 'my-app',
    template: '<h1>HealthMonitor Angular2 App</h1><br/><button (click)="onClickMe()">Click me!</button><br/><br/>{{clickMessage }}'
})
export class AppComponent {
    clickMessage = 'Empty';

    onClickMe() {
        this.clickMessage = 'Clicked. You are my hero!';
    }
}
