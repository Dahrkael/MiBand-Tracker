# MiBand Tracker
Windows (8.1/10) &amp; Windows Phone(8.1/10) app to interact with the Xiaomi Mi Band

## Explanation
This was the first attempt at making the Xiaomi Mi Band work in the Windows environment (more specifically, Windows Phone).
It appeared in technology blogs all around the world!
* http://www.windowscentral.com/third-party-app-xiaomi-miband-wearable-works
* http://www.windowscentral.com/miband-tracker-beta-windows-phone-now-english-and-open-all

After releasing it, some others magically appeared out of nowhere (see: http://www.bindmiband.com/ still in active development),
so I ceased development after reverse-engineering all the features and re-implement them for Windows, since that was the fun part.

It also helped that Xiaomis firmware is pure garbage (and they are a software company? ha!), giving random errors and deleting data if you dont sync the band everyday.

**If you want to know how everything works, or resume development where I left it, go ahead and fork it!**

## Features
I developed it before the white-leds Mi Band and the Mi Band 2 were released, so its fully compatible only with the first batches and probably not with the latest firmwares.

### Done
- Sync steps and sleep data (analyzed, split and saved to mysqlite)
- Realtime steps
- Setting time and date
- Alarms
- Daily target
- selecting leds color
- "Find my band", factory reset, full test

### Pending
- Showing steps and sleep data (the charts and screen are half-done)
- Updating firmware

## License
I release the project under the MIT License, because I'm that kind of guy.
Anyway if you use the code for something cool or you liked it, drop me a line!