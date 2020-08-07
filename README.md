# Transmit.NET
A pure managed C# socket agnostic reliability layer inspired by reliable.io and yojimbo.

Transmit.NET provides a simple and easy-to-use reliability layer designed for use in games built on unreliable UDP connections.

# Features
* Multiple quality-of-service options (reliable, unreliable, fragmented and mixed combinations) for different use cases in a single API
* Lightweight packet acking and packet resending
* Supports messages up to about (almost infinity) large using automatic message fragmentation and reassembly.
* Simple congestion control changes the packet send rate according to round-trip-time.
* GC-friendly for maximum performance.

# Usage
## TODO
