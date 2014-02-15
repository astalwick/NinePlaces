NinePlaces - Travel Planning Website in Silverlight
==========

### About NinePlaces

I began building NinePlaces in 2008, as a way to get my hands dirty with Silverlight 2.0, and mess with some UI/UX ideas.  I saw travel planning as very boring and form-based, and felt that there was an opportunity to build something to make it more fun and accessible.  I had hoped to launch it as a startup, but I came to realize that, while the UI is different and kind of interesting, it's just not something that would have worked for people.  I kept working on it for quite awhile, anyway, as it was a good test-bed for ideas and a good place to experiment with code.

I worked on NinePlaces from 2008 until roughly 2010/11, very part time (2010 was more or less when I fully realized that Silverlight was a dead-end).  It ended up turning into a fairly large project.

### Basic UX

I'm going to put up a video demo of NinePlaces soon, but until then:

The basic interface of NinePlaces is a timeline ribbon that stretches horizontally across the screen.  You can drag left and right to move around in time, and you can zoom in and out on a specific date with the mousewheel.

You plan a vacation by grabbing little 'transportation', 'lodging' or 'activity' icons from an icon bar.  You pin them on to the timeline, add a bit more info (Flight number, duration, for example), and then move on.

The goal was to build a simple and iterative planning tool that would be useful to the user before, during and after the vacation.  Before the vacation, you could plan out your transportation and your lodging (and some activities, if you like).  Print out the itinerary and then head out.  During a vacation, you could simply and easily update the timeline with notes about what you did, attach photos, view your upcoming itinerary, etc.  And after the vacation, you have a permanently stored, visually appealing travel-log.

### Technologies

WCF (for the server)
Silverlight
Amazon SimpleDB (regret that, I do).
MemCached
Amazon S3

