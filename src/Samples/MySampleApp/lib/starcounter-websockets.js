     var ConfigureCommunication = function () {
         var wsImpl = window.WebSocket || window.MozWebSocket;


        window.StarcounterSession = {};

        window.StarcounterSession.SendMessage = function (val)
        {
            this.StableSocket.SendMessage(val);
        }

        window.StarcounterSession.StableSocket =
        {
           PendingIdleCallBacks: { values: [], indexes:{}, keys:[] },
           MaxQueueSize:100,
           OutgoingMessages: new Array(this.MaxQueueSize),
           LastAddedOutgoing:-1,
           LastSentOutgoing:-1,
           LastAcknowledgedOutgoing:-1,
           OutgoingMessageNo: 0,
           UseText: true,

           Connect: function()
           {
//              this.Session.OnMessage.call( this.Session, "Connecting to Starcounter 2.2 Server 81.229.85.23:8181...");
              // create a new websocket and connect
              var ws = new wsImpl('ws://10.211.55.3:8181/consoleappsample', 'my-protocol');
              this.Ws = ws;

              if (!this.UseText)
                 ws.binaryType = "arraybuffer";

              //var ws = new wsImpl('ws://127.0.0.1:8181/consoleappsample', 'my-protocol');
              ws.StableSocket = this;

              // when data is comming from the server, this metod is called
              ws.onmessage = function (evt) {
                 var msg, msgNo, payload;
                 if (this.StableSocket.UseText)
                 {
                     console.log(evt.data);
                     msg = JSON.parse(evt.data);
//                    str = evt.data;
//                    msgNo = window.parseInt(evt.data);
                 }
                 else
                 {
                    str = evt.data.toString();
                    msgNo = new Uint8Array(evt.data)[0];
                 }
                  this.StableSocket.Session.OnTrace( "(ack" + ":" + msg.MsgNo + ")" );
                  this.StableSocket.Session.OnMessage( msg );

                 this.StableSocket.LastAcknowledgedOutgoing = msg.MsgNo;
                 console.log("Acknowledged:" + this.StableSocket.LastAcknowledgedOutgoing);
                 if (this.StableSocket.GetLag()==0)
                 {
                    this.StableSocket.ProcessSendOnIdle();
                 }
              };

              ws.onopen = function( evt)
              {
                  this.StableSocket.Session.OnTrace("connected");
                 ws.IsReady = true;
                 this.StableSocket.Flush();
//                 console.log( this.StableSocket );
              };

              // when the connection is closed, this method is called
              ws.onclose = function () {
                 ws.IsReady = false;
                  this.Session.OnTrace("connection closed");
                 this.StableSocket.Ws = null;
                 this.StableSocket.Connect();
              }

             // when the connection is closed, this method is called
             ws.onerror = function  ( evt ) {
                 this.Session.OnTrace('.. error ' + evt);
                this.StableSocket.Ws = null;
                this.StableSocket.Connect();
             }

           },

           ProcessSendOnIdle: function()
           {
              while ( this.PendingIdleCallBacks.keys.length > 0 )
              {
                 var cb = this.PendingIdleCallBacks.values.shift();
                 var key = this.PendingIdleCallBacks.keys.shift();
                 this.PendingIdleCallBacks.indexes[key] = undefined;
                 cb.Func.call( null, cb.Param );
              }
           },

           SendMessageOnIdle: function( key, callbackobj )
           {
              var index = this.PendingIdleCallBacks.indexes[key];
              if (index)
              {
                 this.PendingIdleCallBacks.values[index] = callbackobj;
              }
              else
              {
                 this.PendingIdleCallBacks.values.push( callbackobj );
                 this.PendingIdleCallBacks.keys.push(key);
                 this.PendingIdleCallBacks.indexes[key] = this.PendingIdleCallBacks.keys.length;
              }
           },

           Flush: function()
           {
              if (this.Ws == undefined)
              {
                 this.Session.OnTrace("Connecting for the first time");
                 this.Connect();
              }
              if (this.Ws.IsReady != true)
                 return;
              try
              {
                 while (this.LastSentOutgoing != this.LastAddedOutgoing )
                 {
                    this.LastSentOutgoing++;
                    console.log("LastSentOutgoing", this.LastSentOutgoing);
                    var msg = this.OutgoingMessages[this.LastSentOutgoing];

                    if (this.UseText)
                    {
                        var snd = { MsgNo: msg.MessageNo, Payload:this.OutgoingMessages[this.LastSentOutgoing].Payload };
                       this.Ws.send( JSON.stringify(snd) );
                    }
                    else
                    {
                       var bin = new Uint32Array(1);
                       bin[0] = msg.MessageNo;
                       this.Ws.send( bin.buffer );
                    }

                  //  out.innerHTML += "(send '" + msg.Payload + "' " + msg.MessageNo + ")";

                 }
              }
              catch (e)
              {
                 this.Session("ERROR: " + e)
//                 inc.innerHTML += 'exception ' + e.message + '<br/>';
                 this.Connect();
              }
           },

           SendMessage: function( val )
           {
              //var lag = this.LagRate();
             // if ( lag <= 0.5 )
             // {
             //    var startTime = (new Date()).getTime();
             //    while ( (((new Date()).getTime() - startTime )) < 1 / lag )
             //    {
             //       this.Ws.read();
             //    }
             // }
              var lag = this.GetLag();
              if (this.IsQueueFull())
                  throw new Exception("Queue full");
              this.LastAddedOutgoing++;
              this.OutgoingMessages[this.LastAddedOutgoing] = { MessageNo:window.StarcounterSession.StableSocket.OutgoingMessageNo, Payload:val };
              this.OutgoingMessageNo++;
              console.log("OutgoingMessageNo", this.OutgoingMessageNo );
              this.Flush();
           },

           IsQueueFull: function()
           {
              if (this.LastAddedOutgoing >= this.LastSentOutgoing)
                 return (this.LastAddedOutgoing - this.LastSentOutgoing >= (this.MaxQueueSize-1) );
              else
                 return ( (this.LastAddedOutgoing + this.MaxQueueSize) - this.LastSentOutgoing >= (this.MaxQueueSize-1) );
           },

           GetLag: function()
           {
              if (this.LastAddedOutgoing >= this.LastAcknowledgedOutgoing)
                 lag =  this.LastAddedOutgoing - this.LastAcknowledgedOutgoing;
              else
                 lag = ( this.LastAddedOutgoing + this.MaxQueueSize ) - this.LastAcknowledgedOutgoing;
//                 if (lag==0)
//                     return 1.0;
//                 rate = 1 / (lag*lag*lag);
//                 console.log("AbsLag=" + lag + " Lagrate=" + rate );
              console.log("AbsLag=" + lag );
              return lag;
           }
/*
           LagRate: function()
           {
              if (this.LastAddedOutgoing >= this.LastAcknowledgedOutgoing)
                 lag =  this.LastAddedOutgoing - this.LastAcknowledgedOutgoing;
              else
                 lag = ( this.LastAddedOutgoing + this.MaxQueueSize ) - this.LastAcknowledgedOutgoing;
              if (lag==0)
                  return 1.0;
              rate = 1 / (lag*lag*lag);
              console.log("AbsLag=" + lag + " Lagrate=" + rate );
              return rate;
           }*/

        }

         window.StarcounterSession.StableSocket.Session = window.StarcounterSession;
//         input.addEventListener('onChange',


     }

    // window.onload = ConfigureCommunication;
     ConfigureCommunication();
