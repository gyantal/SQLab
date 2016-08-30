import { NgModule }      from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';
import { FormsModule }   from '@angular/forms'; // In order to be able to use two-way data binding for form inputs you need to import theFormsModule package in your Angular module. 
import {HttpModule} from '@angular/http';
import { AppComponent }  from './app.component';
@NgModule({
    imports: [BrowserModule, FormsModule, HttpModule],
    declarations: [AppComponent],
    bootstrap: [AppComponent]
})
export class AppModule { }