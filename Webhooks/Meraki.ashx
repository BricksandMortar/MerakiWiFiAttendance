<%@ WebHandler Language="C#" Class="Meraki" %>
// <copyright>
// Copyright 2013 by the Spark Development Network
//
// Licensed under the Rock Community License (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.rockrms.com/license
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>
//

using System;
using Newtonsoft.Json;
using System.Web;
using System.Threading;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using Rock;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;

public class Meraki : IHttpAsyncHandler
{
    public bool IsReusable
    {
        get
        {
            return false;
        }
    }

    public IAsyncResult BeginProcessRequest( HttpContext context, AsyncCallback cb, Object extraData )
    {
        context.Response.Write( "<p>Begin IsThreadPoolThread is " + Thread.CurrentThread.IsThreadPoolThread + "</p>\r\n" );
        AsynchOperation asynch = new AsynchOperation( cb, context, extraData );
        asynch.StartAsyncWork();
        return asynch;
    }

    public void EndProcessRequest( IAsyncResult result )
    {

    }

    public void ProcessRequest( HttpContext context )
    {
        throw new InvalidOperationException();
    }

}

class AsynchOperation : IAsyncResult
{
    private bool _completed;
    private Object _state;
    private AsyncCallback _callback;
    private HttpContext _context;

    // If this acts up try deleting these and going _context.*
    private HttpRequest request;
    private HttpResponse response;

    private const bool ENABLE_LOGGING = true;

    bool IAsyncResult.IsCompleted { get { return _completed; } }
    WaitHandle IAsyncResult.AsyncWaitHandle { get { return null; } }
    Object IAsyncResult.AsyncState { get { return _state; } }
    bool IAsyncResult.CompletedSynchronously { get { return false; } }

    public AsynchOperation( AsyncCallback callback, HttpContext context, Object state )
    {
        _callback = callback;
        _context = context;
        _state = state;
        _completed = false;
    }

    public void StartAsyncWork()
    {
        ThreadPool.QueueUserWorkItem( new WaitCallback( StartAsyncTask ), null );
    }

    private void StartAsyncTask( Object workItemState )
    {
        request = _context.Request;
        response = _context.Response;

        response.ContentType = "text/plain";

        if ( request.HttpMethod != "POST" )
        {
            response.Write( "Invalid request type." );
            return;
        }

        if ( request.ContentType != "application/json" )
        {
            response.Write( "got post with unexpected content type: #{request.media_type}" );
            return;
        }

        if ( request.Form["secret"] != GlobalAttributesCache.Value( "MerakiSecret" ) )
        {
            response.Write( "got post with bad secret: #{map['secret']}" );
            return;
        }

        if ( request.Form["version"] != "2.0" )
        {
            response.Write( "got post with unexpected version: #{map['version']}" );
            return;
        }

        if ( request.Form["type"] != "DevicesSeen" )
        {
            response.Write( "got post for event that we're not interested in: #{map['type']}" );
            return;
        }

        // determine if we should log
        if ( ( !string.IsNullOrEmpty( request.QueryString["Log"] ) && request.QueryString["Log"] == "true" ) || ENABLE_LOGGING )
        {
            WriteToLog();
        }

        if ( request.Form["data"] != null )
        {
            var jsonData = JsonConvert.DeserializeObject<dynamic>( request.Form["data"] );

            var observations = jsonData["observations"];
            if ( observations != null )
            {
                foreach ( var observation in observations.ToList() )
                {
                    MarkAttendance( observation );
                }
                response.StatusCode = 200;

            }
            else
            {
                response.StatusCode = 500;
            }
        }
        else
        {
            response.StatusCode = 500;
        }


        _context.Response.Write( "<p>Completion IsThreadPoolThread is " + Thread.CurrentThread.IsThreadPoolThread + "</p>\r\n" );

        _context.Response.Write( "Hello World from Async Handler!" );
        _completed = true;
        _callback( this );
    }

    private void MarkAttendance( dynamic observation )
    {
        var rockContext = new RockContext();
        string clientMac = observation["clientMac"];
        if ( !String.IsNullOrWhiteSpace( clientMac ) )
        {
            var macAddressValue = new AttributeValueService( rockContext ).Queryable().Where( av =>
                         av.Attribute.Key == "MacAddress" &&
                         av.Value == clientMac )
                         .FirstOrDefault();

            if ( macAddressValue != null && macAddressValue.EntityId != null )
            {
                var attendee = new PersonService( rockContext ).Get( macAddressValue.EntityId.Value );
                if ( attendee != null )
                {
                    var attendanceGroupGuid = GlobalAttributesCache.Value( "MerakiAttendanceGroup" ).AsGuidOrNull();
                    if ( attendanceGroupGuid != null )
                    {
                        var group = new GroupService( rockContext ).Get( attendanceGroupGuid.Value );
                        if ( group != null )
                        {
                            int? personAliasId = new PersonAliasService( rockContext ).GetPrimaryAliasId( attendee.Id );
                            if ( personAliasId.HasValue )
                            {
                                var attendanceDateTime = DateTime.Now;
                                if ( !String.IsNullOrWhiteSpace( observation["seenTime"] ) && observation["seenTime"].AsDateTime() != null )
                                {
                                    attendanceDateTime = observation["seenTime"].AsDateTime().Value;
                                }

                                var attendanceService = new AttendanceService( rockContext );
                                bool alreadyAttended = attendanceService.Queryable().Where( a =>
                                 a.PersonAliasId == personAliasId &&
                                 a.GroupId == group.Id &&
                                 a.StartDateTime.Date == attendanceDateTime.Date )
                                .Any();

                                if ( !alreadyAttended )
                                {
                                    var attendance = new Attendance();
                                    attendance.GroupId = group.Id;
                                    attendance.PersonAliasId = personAliasId;
                                    attendance.StartDateTime = attendanceDateTime;
                                    attendance.CampusId = group.CampusId;

                                    // check that the attendance record is valid
                                    if ( !attendance.IsValid )
                                    {
                                        return;
                                    }

                                    attendanceService.Add( attendance );
                                    rockContext.SaveChanges( true );
                                }
                            }
                        }
                    }
                }
            }
        }
    }

    private void WriteToLog()
    {
        var formValues = new List<string>();
        foreach ( string name in request.Form.AllKeys )
        {
            formValues.Add( string.Format( "{0}: '{1}'", name, request.Form[name] ) );
        }

        WriteToLog( formValues.AsDelimited( ", " ) );
    }

    private void WriteToLog( string message )
    {
        string logFile = HttpContext.Current.Server.MapPath( "~/App_Data/Logs/MerakiLog.txt" );

        // Write to the log, but if an ioexception occurs wait a couple seconds and then try again (up to 3 times).
        var maxRetry = 3;
        for ( int retry = 0; retry < maxRetry; retry++ )
        {
            try
            {
                using ( System.IO.FileStream fs = new System.IO.FileStream( logFile, System.IO.FileMode.Append, System.IO.FileAccess.Write ) )
                {
                    using ( System.IO.StreamWriter sw = new System.IO.StreamWriter( fs ) )
                    {
                        sw.WriteLine( string.Format( "{0} - {1}", RockDateTime.Now.ToString(), message ) );
                    }
                }
            }
            catch ( System.IO.IOException )
            {
                if ( retry < maxRetry - 1 )
                {
                    System.Threading.Thread.Sleep( 2000 );
                }
            }
        }

    }
}