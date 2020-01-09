# SmokeDiffusionPaper
This repository contains the implementation of my paper which will be submitted to Eurographics and Eurovis 2020

![Smoke Screenshot](/smoke.png?raw=true "Smoke Screenshot")
Due to the large project size, you can direct download the project by following this link:https://drive.google.com/file/d/18GNlwmyPq_QluqMw6hMLVUIr313RZ8x1/view?usp=sharing

You need Unity3D game engine to run the project.After you have added and loaded it, you can immediately press play. At the start, it needs to load all the 3D textures in once(total 3.91GB), so depending on the reading speed of your disk, you have to wait (2-5 minutes) to load.

The current project has been tested on oculus and htc vive pro. If you wish to test it yourself on VR, you have to download manually the steamVR plugin from the asset store and enable it. No further modifications are required.
Link to a short demo : https://drive.google.com/file/d/1VLudK7kvtxjiHhHXAlzCid-6ZcN9gJAb/view?usp=sharing

In the src folder, the source code of shaders and scripts is provided.

 
Known Issues(will be fixed at the next edition):
 1. Shadowmap is not been used to guide the light marching.
 2. Gaussian Blur diminishes the smoke's edges with the opaque geometry.
 
