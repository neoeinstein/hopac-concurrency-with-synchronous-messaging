(**
- title : Powering Concurrency with Synchronous Messaging
- description : One of the four tenets in the Reactive Manifesto is asynchronous messaging, but what if we considered the alternative? Synchronous messaging may not be the sin we've all been warned against. Inspired by Concurrent ML and the concurrent synchronous processes model, we will discuss the concurrency primitives exposed by the Hopac library and how they can be composed to build highly-concurrent applications which benefit from lightweight threads, an optimized work scheduler, and the ability to react to multiple communication channels. All while avoiding some of the pitfalls of asynchronous messaging.
- author : Marcus Griep
- theme : night
- transition : page

***

*)
(*** hide ***)
#I @"..\packages\Hopac\lib\net45"
#r "Hopac.Core.dll"
#r "Hopac.dll"
#r "Hopac.Platform.dll"
open Hopac
open Hopac.Infixes
let always x = (fun _ -> x)
(*** hide,define-output:warning ***)
run <| Job.unit ()
(**

# Hopac

## Powering Concurrency with Synchronous Messaging

<br/>
<br/>

### Marcus Griep
<div>
[@neoeinstein](https://twitter.com/neoeinstein)
</div>

<div style="float:right;position:fixed;bottom:-100px;right:-100px;text-align:right">
Slides online:<br/>
<a href="https://bit.ly/HopacLambdaConf2016">bit.ly/HopacLambdaConf2016</a>
</div>

' 0:45
' Introduce self
' Lead software engineer with Cimpress
' Lead a squad of engineers

***
## The Reactive Mainfesto

<ul style="font-size:125%">
<li class="fragment fade-down">Responsive</li>
<li class="fragment fade-down">Resilient</li>
<li class="fragment fade-down">Elastic</li>
<li class="fragment fade-down">Message-Driven</li>
</ul>

' 2:00
' Model for systems architecture
' Millisecond response times
' 100% uptime/fault-tolerance
' Highly-scalable systems
' Evolve over time

---
### Message-Driven

Reactive Systems rely on <span class="fragment highlight-blue" data-fragment-index="2">asynchronous message-passing</span> to <span class="fragment highlight-green" data-fragment-index="4">establish a boundary between
components</span> that ensures loose coupling, isolation and location transparency. <span class="fragment fade-out" data-fragment-index="1">This boundary also provides the means to
delegate failures as messages. Employing explicit message-passing enables load management, elasticity, and flow control
by shaping and monitoring the message queues in the system and applying back-pressure when necessary. Location
transparent messaging as a means of communication makes it possible for the management of failure to work with the same
constructs and semantics across a cluster or within a single host.</span> <span class="fragment highlight-blue" data-fragment-index="3">Non-blocking communication</span>
allows recipients to <span class="fragment highlight-green" data-fragment-index="5">only consume resources while active</span>, leading to less system overhead.

' 3:00
' Cut out cruft and focus
' Async & non-blocking
' What reasons does the reactive manifesto give for async and non-blocking
' Asynchronous message-passing: establishes component boundaries
' Non-blocking: threads don't consume resources while idle

---
## Loopholes!

<br/>

<h3 class="fragment" style="color:rgb(45,189,75)"><em>Synchronous messaging inside a component!</em></h3>

<br/>

<dl>
<dt class="fragment">Asynchronous message-passing</dt>
<dd class="fragment fade-down">Establishes component boundaries</dd>
<br/>
<dt class="fragment">Non-blocking communication</dt>
<dd class="fragment fade-down">Threads don't consume resources while idle</dd>
</dl>

<div class="fragment fade-up" style="margin-top:1.5em;font-size:80%;color:rgb(215,215,215)">Note: A "thread" refers to a thread of execution,<br/><em>not an operating system thread.</em></div>

' 4:00

---

## The Reactive Manifesto

<div class="fragment fade-up" style="color:rgb(45,90,180);font-size:200%;font-weight:bold;margin-top:2em"><em>Still not a law!</em></div>

***
### Asynchronous message-passing

#### Postal System

<div>
<img src="images/mailbox.jpg" alt="Mailbox" width="350" />
</div>

<ul>
<li class="fragment fade-down">Unbounded Mailboxes</li>
<li class="fragment fade-down">Processes <code>send</code> messages</li>
<li class="fragment fade-down">At-most-once delivery</li>
<li class="fragment fade-down">Better guarantees, at a cost</li>
<li class="fragment fade-down">Implemented by actor systems:
<ul>
<li>Akka.net</li><li>Orleans</li>
</ul></li>
</ul>

' 5:00
' Characterized by:
' Recipients have unbounded mailboxes (or bounded boxes that overflow as dead letters)
' Once sent, sender doesn't have view of the message status
' Getting better guarantees requires tracking of some sort, either by the recipient or system
' Existing frameworks: Akka/.Net, Orleans

---
### Synchronous message-passing

#### Rendezvous Point

<div>
<img src="images/ISO_7010_E007.svg" alt="Meeting Point" width="250" />
</div>

<ul>
<li class="fragment fade-down">Channels</li>
<li class="fragment fade-down">Processes <code>give</code> and <code>take</code> messages</li>
<li class="fragment fade-down">Exactly-once delivery</li>
<li class="fragment fade-down">Examples include:
<ul>
<li>Concurrent ML / Go / Racket</li>
<li>Hopac / Clojure's core.async</li>
</ul>
</li>
</ul>

' 6:00
' No buffer
' Communication occurs over a channel and requires both parties to a communication to be there
' Offers to give or take can be withdrawn if another communication alternative is available
' Don't want to wait for the counterparty to receive your message? That's ok. Spawn a process to wait for you instead.
' I will refer to it as synchronous rendezvous

***
## Why synchronous rendezvous?

<ul>
<li class="fragment">Sender <em>and</em> recipient share common knowledge of transmission</li>
<li class="fragment">Failure mode is typically deadlock</li>
<li class="fragment">Semantics of local and distributed interactions are different</li>
</ul>

' 10:00
' Async has low overhead, but no sync info to sender
' I don't like deadlock!!
' Alternative: Error detection delayed until buffer exhausted (or memory)
' With local comm, client can know if server accepted request
' Because async is a weaker mechanism, it reduces local case to distributed case
' Some think this is an argument in favor of async
' Should provide abstraction for interacting with server
' Semantics of that abstraction are different from distributed case
' Dirty little secret: Akka/.net "optimizes" local communication
' Within a process, Akka.net optimizes to directly delivering messages, avoiding the postal system, but that's
' like going to the postbox and dropping off a letter for your next-door neighbor. The postman optimizes the process,
' bringing the letter directly to your neighbor's mailbox. Meanwhile, your neighbor has been standing next to his
' mailbox eagerly awaiting a new message. As soon as the postman drops off the letter, your neighbor springs into
' action to process the message. If you are counting on a reply, you've included your own self-addressed stamped
' envelope, and your neighbor repeats the process to send you the reply while you stand next to your mailbox.

---
### Local communication with asynchronous messaging

<ul>
<li class="fragment">Drop off letter in postbox</li>
<li class="fragment">Mail carrier recognizes the letter is local<div class="fragment fade-down">and goes to the address</div></li>
<li class="fragment">Mail carrier hand-delivers it<div class="fragment fade-down">to your neighbor's mailbox</div><div class="fragment fade-down">while your neighbor watches</div></li>
<li class="fragment">Your neighbor pulls the letter out of the mailbox</li>
</ul>

' What if you need a reply?

---
### Local communication with synchronous rendezvous

<ul>
<li class="fragment">You go to rendezvous location<div class="fragment fade-down">and hand the letter to your neighbor</div></li>
</ul>

' What if you need a reply?

***
## [Hopac](https://hopac.github.io/Hopac/Hopac.html)

<ul>
<li class="fragment fade-down">F# library <small class="fragment fade-left">(not a framework)</small></li>
<li class="fragment fade-down">Inspired by Concurrent ML</li>
<li class="fragment fade-down">Uses synchronous rendezvous</li>
<li class="fragment fade-down">Provides composable primitives</li>
<li class="fragment fade-down">Cares about scalabilty</li>
</ul>

<div class="fragment fade-down" style="margin-top:1em">

    [lang=text]
    WARNING: You are using single-threaded workstation garbage
    collection, which means that parallel programs cannot scale.
    Please configure your program to use server garbage collection.
</div>

' 12:00
' Now let's move on and talk about Hopac.

***
### Comparison to F# Async

<div class="fragment">

<div style="font-size:125%">F# Async</div>

<ul>
<li class="fragment fade-down">Run on threadpool threads</li>
<li class="fragment fade-down">Default threadpool scheduler</li>
</ul>
</div>

<br />

<div class="fragment">
<div style="font-size:125%">Hopac</div>

<ul>
<li class="fragment fade-down">Lightweight threads: <code>Job<'x></code></li>
<li class="fragment fade-down">Run on dedicated Hopac threads</li>
<li class="fragment fade-down">Cooperative scheduler</li>
</ul>
</div>

' One Hopac thread per logical processor


---
### Using `async{}`

*)

let delayValueAsync prior = async {
  let! x = prior
  do! Async.Sleep 1
  return x
}
let printAsync x = async {
  printfn "%i" x
}

Async.Start <| Async.Sleep 1
Async.StartImmediate <| printAsync 23 
Async.RunSynchronously <| delayValueAsync (async.Return 4)

(**
---
### Using `job{}`

*)

let delayValueJob prior = job {
  let! x = prior |> asJob
  do! Async.Sleep 1
  return x
}
let printJob x = job {
  printfn "%i" x
}

queue <| timeOutMillis 1
start <| printJob 23 
run <| delayValueJob (Job.result 4)

(**

' 13:00
' Note use of `asJob`
' Provides bind for `async`, `job`, and `Task`

---
### Actor look-a-like
*)
(*** module:actor ***)
type CellMsg =
  | SetValue of string
  | GetValue of IVar<string>
(**
<div class="fragment">
*)
(*** module:actor ***)
let spawnCellActor initialValue : Job<Ch<CellMsg>> = job {
  let commCh = Ch ()
  do! Job.iterateServer initialValue <| fun x -> job {
    let! msg = Ch.take commCh
    match msg with
    | SetValue v -> return v
    | GetValue reply ->
      do! IVar.fill reply x
      return x
  }
  return commCh
}
(**
</div>

---
### Actor look-a-like

*)
(*** module:actor ***)
// MailboxProcessor.Post
let send mbx msg = job {
  return! Ch.send mbx msg
}
// MailboxProcessor.PostAndAsyncReply
let sendAndReceive mbx toMsg = job {
  let reply = IVar ()
  do! Ch.send mbx (toMsg reply)
  return! IVar.read reply
}
(**
---
### Actor look-a-like

Test it out
*)
(*** module:actor,define-output:actor2 ***)
run <| job {
  let! cell = spawnCellActor "Henry"
  let! n1 = sendAndReceive cell GetValue
  do! send cell (SetValue "Todd")
  let! n2 = sendAndReceive cell GetValue
  printfn "Name at 1: %s" n1
  printfn "Name at 2: %s" n2
}
(**
<div class="fragment">
Output:
*)
(*** include-output:actor2 ***)
(**
</div>

***
### Alternatives

<ul>
<li class="fragment fade-down">Composable communication primitive: <code>Alt<'x></code></li>
<li class="fragment fade-down"><code>Alt</code> is where the magic happens</li>
<li class="fragment fade-down">Quantum superposition of waits</li>
<div class="fragment fade-down">
*)
(*** hide ***)
type Msg = NetworkMessage of string | Timeout
let networkComm = timeOutMillis 1 ^->. "Message"
(*** show ***)
Alt.choose
  [ networkComm |> Alt.afterFun NetworkMessage
    timeOutMillis 250 |> Alt.afterFun (always Timeout) ]
(**
</div>
</ul>
<div class="fragment fade-down">
<img src="images/The_Dalek_Interpretation.jpg" alt="Superposition of waits" />
</div>

' 15:00
' Synchronous rendezvous demands a way to select between communication alternatives
' Alternatives provide a way to wait for different possible events
' Produce more `Alt`s when composed
' Alternatives give ability to select program flow based on concurrent state
' Will be trying to avoid some of the infixes in this presentation, but they can increase expresiveness

---
### Alternatives

Selection in three stages
<ul>
<li class="fragment fade-down">Instantiated<div class="fragment fade-down"><code>Alt.prepareJob</code> / <code>guard</code></div></li>
<li class="fragment fade-down">Available</li>
<li class="fragment fade-down">Committed <div class="fragment fade-right" style="display:inline-block"><em>or not</em></div><div class="fragment fade-down"><code>Alt.afterJob</code> / <code>wrap</code></div></li>
</ul>

' 16:00
' Conceptually, an alternative operation is *instantiated* by a consumer and then may
' become available. When an alternative becomes available, a consumer can *commit* to
' the result of that communication.
' An alternative also knows when it has not been selected, important for negative acknowledgements

---
### Alternatives in Hopac

<ul>
<li class="fragment fade-down">Emphasizes performance over fairness</li>
<li class="fragment fade-down">Lazy alternative instantiation</li>
</ul>

<div class="fragment fade-down">
*)
(*** define-output:altPerformance ***)
let alwaysWithPrepare x =
  Alt.prepareFun <| fun () ->
    printfn "Instantiated %i" x
    Alt.always x

let resultPerf =
  Alt.choose
    [ alwaysWithPrepare 0
      alwaysWithPrepare 1 ]

run (resultPerf |> Job.map (printfn "Selected case #%i"))
(**
</div>
<div class="fragment fade-down">
Output:
*)
(*** include-output:altPerformance ***)
(**
The second case never gets instantiated.
</div>

' Concurrent ML eagerly instantiates all alternatives and uses a fair choice
' Hopac emphasizes performance over fairness, and uses lazy alternative instantiation
' So if we run this 3 times

***

## Servers

<div class="fragment fade-up" style="font-size:150%;margin-top:2em">
<em>Like actors, but more powerful!</em>
</div>

---
### Servers

<div style="font-size:125%">Reference Cell</div>

*)
(*** module:cell1 ***)
type Cell<'a> =
  { getCh: Ch<'a>
    putCh: Ch<'a> }
(**

<div class="fragment fade-down" style="font-size:80%;margin-bottom:1em">
<code>Ch<'a></code>: two-way communication: <code>give</code>/<code>take</code>
</div>

---
### Servers

The server implementation
*)
(*** module:cell1 ***)
let cell x = job {
  let c = {getCh = Ch (); putCh = Ch ()}
  do! Job.iterateServer x <| fun x ->
        Alt.choose
          [ Ch.take c.putCh
            Ch.give c.getCh x |> Alt.afterFun (always x) ]
  return c
}
(**

<div class="fragment fade-down">
Abstract the protocol
*)
(*** module:cell1 ***)
let put c v = Ch.give c.putCh v
let get c = Ch.take c.getCh
(**
</div>

---
### Servers

Test it out
*)
(*** module:cell1,define-output:ref-cell ***)
run <| job {
  let! myCell = cell "Henry"
  let! n1 = get myCell
  do! put myCell "Todd"
  let! n2 = get myCell
  printfn "Name at 1: %s" n1
  printfn "Name at 2: %s" n2
}
(**
<div class="fragment fade-down">
Output:
*)
(*** include-output:ref-cell ***)
(**
</div>

***
## Handling Replies

<div class="fragment" style="font-size:150%;margin-top:2em">
<em>Talking back to your clients</em>
</div>

---
### Handling Replies

Create a cell that can handle replies
*)
(*** module:cell2 ***)
type CellWithReply<'a> =
  { getCh: Ch<'a>
    putCh: Ch<'a * IVar<'a>> }
(**

<div class="fragment fade-down" style="font-size:80%;margin-bottom:1em">
<code>IVar<'a></code>: write-once channel: <code>fill</code>/<code>read</code>
</div>

---
### Handling Replies

Update the server
*)
(*** module:cell2 ***)
let handlePut oldVal (newVal, reply) =
    IVar.fill reply oldVal |> Job.map (always newVal)

let cell x = job {
  let c = {getCh = Ch (); putCh = Ch ()}
  do! Job.iterateServer x <| fun x ->
        Alt.choose
          [ Ch.take c.putCh   |> Alt.afterJob (handlePut x)
            Ch.give c.getCh x |> Alt.afterFun (always x) ]
  return c
}
(**

' Added an IVar to the put channel, allows us to send a reply back to the client
' Create this handlePut function that will fill the reply with the old value
' and then return the new state.
' IVar is optimized for this use case.

---
### Handling Replies

Update the protocol abstraction
*)
let get c = Ch.take c.getCh
let put c v =
  Alt.prepareFun <| fun () ->
    let reply = IVar ()
    Ch.give c.putCh (v,reply)
    |> Alt.afterJob (fun () -> IVar.read reply)
(**

<div class="fragment" style="font-size:80%;margin-bottom:1em">
Commit on <code>give</code>
</div>

' Use prepareFun because now we need to create the IVar that we give to the
' the server. This is similar to the F# AsyncReplyChannel<'a>

' Synchronization point is when we give the new value to the reference cell
' But we want to synchronize on the server's reply instead
' In other words, we want the server to synchronize committing to the change
' when the client commits to receiving the reply.

<div class="fragment fade-down">
Rework the synchronization point
*)
(*** module:cell2 ***)
let putSyncOnReply c v =
  Alt.prepareJob <| fun () ->
    let reply = IVar ()
    Ch.send c.putCh (v,reply)
    |> Job.map (fun () -> IVar.read reply)
(**
</div>

<div class="fragment" style="font-size:80%;margin-bottom:1em">
Commit on <code>read</code>
</div>

' So we switch to prepareJob, and asynchronously send the message to the server,
' then return an alternative that synchronizes on the reply.
' So that's good. We can now have a choice

---
### Handling Replies

Test it out
*)
(*** module:cell2,define-output:commit-on-send ***)
run <| job {
  let! myCell = cell "Henry"
  do!
    Alt.choose
      [ putSyncOnReply myCell "Todd"
          |> Alt.afterFun (printfn "Old value: %s")
        timeOutMillis 0
          |> Alt.afterFun (fun () -> printfn "Timed out…") ]

  let! value = get myCell
  printfn "Current value: %s" value
}
(**
<div class="fragment fade-down">
Output
*)
(*** include-output:commit-on-send ***)
(**
</div>

<div class="fragment fade-up">
This is <em>not</em> the semantic we want
</div>

' What happens if the server has been busy and we hit the timeout? We've already sent the
' message. The server has still committed to the change when it receives the message. This
' is similar to the way that a message is effectively committed to once sent in the actor model.
' But we can do better. We can inform the server in the event we don't commit to action.

***

## Negative Acknowledgements

<div class="fragment" style="font-size:150%;margin-top:2em">
<em>Keeping your options open</em>
</div>

---
### Negative Acknowledgements

Create a new cell type
*)
(*** module:cell2 ***)
type CellWithNack<'a> =
  { getCh: Ch<'a>
    putCh: Ch<'a * Promise<unit> * Ch<'a>> }
(**

<div class="fragment fade-down" style="font-size:80%;margin-bottom:1em">
<code>Promise<'a></code>: offer to provide a value at a future time: <code>start</code>/<code>read</code>
</div>

---
### Negative Acknowledgements

Update the server so that it commits when<br/>the client accepts the reply or sends a nack
*)
(*** module:cell2 ***)
let handlePut oldVal (newVal, nack, replyCh) =
  Alt.choose
    [ Ch.give replyCh oldVal |> Alt.afterFun (always newVal)
      Promise.read nack      |> Alt.afterFun (always oldVal) ]

let cell x = job {
  let c = {getCh = Ch (); putCh = Ch ()}
  do! Job.iterateServer x <| fun x ->
        Alt.choose
          [ Ch.take c.putCh   |> Alt.afterJob (handlePut x)
            Ch.give c.getCh x |> Alt.afterFun (always x) ]
  return c
}
(**

---
### Negative Acknowledgements

Update the client abstraction
*)
let get c = Ch.take c.getCh
let putWithNack c v =
  Alt.withNackJob <| fun nack -> job {
    let replyCh = Ch ()
    do! Ch.send c.putCh (v,nack,replyCh)
    return Ch.take replyCh
  }

(**
---
### Negative Acknowledgements

Test it out
*)
(*** module:cell2,define-output:commit-on-client ***)
run <| job {
  let! myCell = cell "Henry"
  do!
    Alt.choose
      [ putWithNack myCell "Todd"
          |> Alt.afterFun (printfn "Old value: %s")
        timeOutMillis 0
          |> Alt.afterFun (fun () -> printfn "Timed out…") ]

  let! value = get myCell
  printfn "Current value: %s" value
}
(**
<div class="fragment fade-down">
Output:
*)
(*** include-output:commit-on-client ***)
(**
</div>

<div class="fragment fade-up">
This has the semantic we want
</div>

---
## Negative Acknowledgements

<div class="fragment fade-right" style="font-size:125%;margin-top:2em">
<em>Allow the client and server to collaborate</em>
</div>

<div class="fragment fade-left" style="font-size:125%;margin-top:1em;margin-bottom:1em">
<em>Enable the client to explore multiple options</em>
</div>

<div class="fragment fade-up" style="font-size:125%">
<em>Very complicated to implement with asynchronous messaging</em>
</div>

***
## Infix Operators

<div class="fragment" style="font-size:150%;margin-top:2em" data-fragment-index="1">
<div class="fragment grow" style="font-size:90%;color:rgb(229,50,50)" data-fragment-index="1">
<em>Warning: Crazy custom operators inbound</em>
</div>
</div>

---
### Infix Operators

<div class="opListGroup">
<div class="opListColumn messaging">
<div class="opListHeader">Messaging</div>
<dl class="opList">
<dt><code>*<-</code></dt><dd>Ch.give</dd>
<dt><code>*<+</code></dt><dd>Ch.send</dd>
<dt><code>*<=</code></dt><dd>IVar.fill</dd>
<dt><code>*<=!</code></dt><dd>IVar.fillFailure</dd>
<dt><code>*<<=</code></dt><dd>MVar.fill</dd>
<dt><code>*<<+</code></dt><dd>Mailbox.send</dd>
</dl>
<div class="opListHeader">Patterns</div>
<dl class="opList patterns">
<dt><code>*<+->=</code></dt><dd>Nack Job</dd>
<dt><code>*<+->-</code></dt><dd>Nack Fun</dd>
<dt><code>*<-=>=</code></dt><dd>IVar Job<sup>^</sup></dd>
<dt><code>*<-=>-</code></dt><dd>IVar Fun<sup>^</sup></dd>
<dt><code>*<+=>=</code></dt><dd>IVar Job<sup>+</sup></dd>
<dt><code>*<+=>-</code></dt><dd>IVar Fun<sup>+</sup></dd>
</dl>
</div>
<div class="opListColumn actions">
<div class="opListHeader">Actions</div>
<dl class="opList">
<dt><code>^=></code></dt><dd>Alt.afterJob</dd>
<dt><code>^-></code></dt><dd>Alt.afterFun</dd>
<dt><code>^=>.</code></dt><dd>Alt.afterJob<sup>i</sup></dd>
<dt><code>^->.</code></dt><dd>Alt.afterFun<sup>i</sup></dd>
<dt><code>^->!</code></dt><dd>Alt.raises</dd>
</dl>
</div>
<div class="opListColumn choice">
<div class="opListHeader">Choice</div>
<dl class="opList">
<dt><code><|></code></dt><dd>Alt.choose</dd>
<dt><code><~></code></dt><dd>Alt.chooser</dd>
<dt><code><|>*</code></dt><dd>Alt.choose<sup>*</sup></dd>
</dl>
<div class="opListHeader" style="margin-top:1em">Combine</div>
<dl class="opList">
<dt><code><&></code></dt><dd>Pair sequential</dd>
<dt><code><*></code></dt><dd>Pair parallel</dd>
<dt><code><+></code></dt><dd>Pair alts</dd>
</dl>
</div>
<div class="opListColumn sequencing">
<div class="opListHeader">Sequencing</div>
<dl class="opList">
<dt><code>>>=</code></dt><dd>Job.bind</dd>
<dt><code>>>=*</code></dt><dd>Job.bind<sup>*</sup></dd>
<dt><code>>>-</code></dt><dd>Job.map</dd>
<dt><code>>>-*</code></dt><dd>Job.map<sup>*</sup></dd>
<dt><code>>>=.</code></dt><dd>Job.bind<sup>i</sup></dd>
<dt><code>>>=*.</code></dt><dd>Job.bind<sup>i,*</sup></dd>
<dt><code>>>-.</code></dt><dd>Job.map<sup>i</sup></dd>
<dt><code>>>-*.</code></dt><dd>Job.map<sup>i,*</sup></dd>
<dt><code>>=></code></dt><dd>Compose</dd>
<dt><code>>=>*</code></dt><dd>Compose<sup>*</sup></dd>
<dt><code>>-></code></dt><dd>Composs/Map</dd>
<dt><code>>->*</code></dt><dd>Compose/Map<sup>*</sup></dd>
</dl>
</div>
</div>

<div class="opListFooter">
<div class="footnote">i: Ignore result of previous op</div>
<div class="footnote">*: Memoize as <code>Promise</code></div>
<div class="footnote">^: Sync on giving message</div>
<div class="footnote">+: Sync on reading reply</div>
</div>

---
### Infix Operators

<div class="fragment fade-down">
Server
*)
(*** module:cell2 ***)
let handlePut oldVal (newVal, nack, replyCh) =
      replyCh *<- oldVal ^->. newVal
  <|> nack               ^->. oldVal

let cell x = job {
  let c = {getCh = Ch (); putCh = Ch ()}
  do! Job.iterateServer x <| fun x ->
        c.putCh       ^=> handlePut x
    <|> c.getCh *<- x ^->. x
  return c
}
(**
</div>

<div class="fragment fade-down">
Client
*)
let get c = Ch.take c.getCh
let putWithNack c v =
    c.putCh *<+->- fun replyCh nack -> (v, nack, replyCh)
(**
</div>

---
### Infix Operators

Test it out
*)
(*** module:cell2,define-output:commit-on-client ***)
run <| job {
  let! myCell = cell "Henry"
  do!
        putWithNack myCell "Todd" ^-> printfn "Old value: %s"
    <|> timeOutMillis 0 ^->. printfn "Timed out…"

  let! value = get myCell
  printfn "Current value: %s" value
}
(**
<div class="fragment fade-down">
Output:
*)
(*** include-output:commit-on-client ***)
(**
</div>

***
## Supervision

<div class="fragment" style="font-size:150%;margin-top:2em">
<em>But actor systems come with supervisors!</em>
</div>

---
### Simple supervision

<div class="fragment">
Server that can fail
*)
let failableServer ch =
  Job.forever <| job {
    printfn "Ready for next message"
    let! value = Ch.take ch
    if value < 0 then
      failwith "Negative values make me sad"
    else
      printfn "Received %i" value
  }
(**
</div>

<div class="fragment fade-up">
This server will throw an exception if it receives negative values
</div>

---
### Simple supervision

<div class="fragment fade-down">
*)
let withSupervisor supervise job =
  supervise job

let rec restartOnException onRestart server = job {
  try
    do! server |> asJob
  with exn ->
    printfn "Child had exception; restarting: %s: %s"
      (exn.GetType().Name)
      exn.Message
    do! onRestart |> asJob
    return! restartOnException onRestart server
}
(**
</div>

---
### Simple supervision

Test it out
*)
(*** define-output:evil ***)
run <| job {
  let evilCh = Ch ()
  let supervisor =
    restartOnException <| timeOutMillis 10
  let server =
    failableServer evilCh
    |> withSupervisor supervisor
  do! Job.start server
  do! Ch.give evilCh 10
  do! Ch.give evilCh -10
  do! Ch.give evilCh 20
}
(**
<div class="fragment fade-down">
Output:
*)
(*** include-output:evil ***)
(**
</div>

***
## Distribution

<div class="fragment fade-right" style="margin-top:2em">
No story here
</div>

<div class="fragment fade-left" style="margin-top:1em;margin-bottom:1em">
Hopac is not designed for this use case
</div>

<div class="fragment fade-up">
Look elsewhere for your distribution story
</div>

***
## Pre-emption

<div class="fragment fade-right" style="margin-top:2em">
Hopac's scheduler is cooperative
</div>

<div class="fragment fade-left" style="margin-top:1em;margin-bottom:1em">
It will not pre-empt threads like the OS scheduler
</div>

<div class="fragment fade-up">
You can do uncooperative things that starve threads
</div>

***
## The Reactive Manifesto

<div class="fragment fade-right" style="margin-top:2em">
Synchronous rendezvous is not at odds with the Rective Manifesto
</div>

<div class="fragment fade-left" style="margin-top:1em;margin-bottom:1em">
The Reactive Manifesto focuses on systems architecture problems
</div>

<div class="fragment fade-up">
Inside a component, synchronous rendezvous is fine
</div>

***
### Hopac's Power

<ul>
<li class="fragment fade-down">Lightweight threads</li>
<li class="fragment fade-down">Garbage collection friendly</li>
<li class="fragment fade-down">Minimal overhead to server–client interactions</li>
<li class="fragment fade-down">Context switches between threads are highly optimized</li>
<li class="fragment fade-down">Composable primitives</li>
</ul>

<ul style="list-style:none;margin-top:1em">
<li class="fragment fade-down"><code>Ch<'a></code> is an <code>Alt<'a></code></li>
<li class="fragment fade-down"><code>IVar<'a></code> is a <code>Promise<'a></code> <div class="fragment fade-right" style="display:inline-block">is an <code>Alt<'a></code></div></li>
<li class="fragment fade-down"><code>MVar<'a></code> is an <code>Alt<'a></code></li>
<li class="fragment fade-down"><code>Mailbox<'a></code> is an <code>Alt<'a></code></li>
</ul>


<div class="fragment fade-up" style="margin-top:1em">
<code>Alt<'a></code>s compose to <code>Alt<'a></code>
</div>

***
### Hopac Streams

<ul>
<li class="fragment fade-right">Non-deterministic stream of values <div class="fragment fade-right" style="display:inline-block">(choice stream)</div></li>
<li class="fragment fade-right">Similar to Rx observable sequences</li>
<li class="fragment fade-right">Handle same operations as ordinary lazy streams<div class="fragment fade-down">Provides <code>Stream.foldBack</code> and <code>Stream.groupByFun</code></div></li>
<li class="fragment fade-right">Consistent<div class="fragment fade-down">Every consumer gets the exact same sequence of values</div><div class="fragment fade-down">No need for replay</div></li>
<li class="fragment fade-right">Pull-based<div class="fragment fade-down">Puts the consumer of the stream in control</div></li>
</ul>

' Briefly touched on

***
## Resources

- [Hopac][]
 - [Programming Guide][HopacProg]
 - [Cancellation of Async on Negative Acknowledgement][HopacAsync]
- [Go][]
- [core.async][]

<div style="float:right">
<p><strong>Marcus Griep</strong><br/>
[@neoeinstein][]<br/>
[neoeinstein.github.io](https://neoeinstein.github.io/)</br>
neoeinstein@gmail.com</p>
</div>

  [RPik12v]:https://vimeo.com/49718712
  [Hopac]:https://hopac.github.io/Hopac/Hopac.html
  [HopacProg]:https://github.com/Hopac/Hopac/blob/master/Docs/Programming.md
  [HopacAsync]:https://github.com/Hopac/Hopac/blob/master/Docs/Alternatives.md
  [Go]:http://golang.org/
  [core.async]:http://clojure.com/blog/2013/06/28/clojure-core-async-channels.html
  [@neoeinstein]:https://twitter.com/neoeinstein
*)