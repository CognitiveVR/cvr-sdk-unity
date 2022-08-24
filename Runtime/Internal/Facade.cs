using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//internal access to static dll functions for serializing data
//should dll handle concept of 'sessions' too?
//should facade have a reference to a session here? multiple sessions at once?


//customevent.send("whatever",vector3.zero)
//send(args){ facade.SubmitCustomEvent(args);}


//what does 'core' do?
    //merge with session manager
//what does 'sessionManager' do?
    //start
    //end
    //unity scene change callbacks
    //set session properties
//gameplay references
    //runtime accessible stuff used throughout the SDK. and scene references go here

//this connects to the dll for serializing and formating stuff
public static class FacadeFunctions
{
    
}
