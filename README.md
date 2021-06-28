# Volumetric Renderer

## Description
This program renders volumetrically a 3D object using 3D data and the basic volume ray casting algorithm (see image below), with some additional features added. Another main feature of this application is that it can render dynamic volumetric smoke in real time, using the right parameters with pseudo-random generated data.

Lab project for the _UPF_ course _Advanced Computer Graphics_.

![IMAGE ALT TEXT HERE](https://upload.wikimedia.org/wikipedia/commons/thumb/7/76/Volume_ray_casting.svg/600px-Volume_ray_casting.svg.png)  
Source: [Wikipedia](https://en.wikipedia.org/wiki/Volume_ray_casting)

## Demo
[![IMAGE ALT TEXT HERE](https://img.youtube.com/vi/xeTilEstYr4/0.jpg)](https://www.youtube.com/watch?v=xeTilEstYr4)

## Other features
Additional features:
- Variable quality (length of the step vector)
- Variable brightness
- Color
- Jittering
- Data values threshold
- Transfer function (using data values / gradient / values + gradient via LUT)
- Local Illumination
- Global Illumination

## Software Engine
This program has been developed using the framework provided by Javi Agenjo (in C++ and OpenGL) and with the assistance of Miquel Clark (UPF teacher), from the UPF course _Advanced Computer Graphics_.

## Contributions
**Software Developers**: Santi Paprika, [Genís Plaja](https://github.com/genisplaja)     
**Framework developer**: [Javi Agenjo](https://github.com/jagenjo) (UPF teacher)     
**Assistance**: Miquel Clark (UPF teacher)