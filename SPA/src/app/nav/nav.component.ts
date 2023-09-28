import { Component, OnInit } from '@angular/core';
import { AccountService } from '../_services/account.service';
import { R3SelectorScopeMode } from '@angular/compiler';

@Component({
  selector: 'app-nav',
  templateUrl: './nav.component.html',
  styleUrls: ['./nav.component.css']
})
export class NavComponent implements OnInit{
  model: any= {};
  loggedIn= false;

  constructor(private accountService: AccountService){}

  ngOnInit(): void{
    this.getCurrentUser(); 
  }

  getCurrentUser(){
    this.accountService.currentUser$.subscribe({
      next: user => this.loggedIn = !!user,
      error: error => console.log(error)
    });
  }
  
  login(): void{
    this.accountService.login(this.model).subscribe({
      next:(response) => {
        console.log(response);
        this.loggedIn = true;
      },
      error: error=> {console.log(error)}
    })
  }

  logout(): void{
    this.accountService.logout();
    this.loggedIn = false;
  }
}