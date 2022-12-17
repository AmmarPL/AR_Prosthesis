# AR_Prosthesis

This project is built on top of https://github.com/luxonis/depthai-unity/tree/main/OAKForUnity/URP.

## Getting Started
This repository contains prototype code that mirrors the actions of one arm on the other arm. Movenet is a body pose estimation model by Google Research. Here we use it to detect the key points in the two arms. We use an Oak-D camera and build on top of the library by Luxonis to implement our code.

## Requirements
To run this prototype, you'll need the following:
1. An Oak-D Camera - we specifically used the Oak-D Lite camera.
2. Unity - the prototype was tested with Unity version 2021.3.11f1.

## Running the prototype
1. Clone the repo: git clone https://github.com/AmmarPL/AR_Prosthesis
2. Open the downloaded folder using Unity
3. Go to Assets -> Plugins -> OakForUnity -> Example Scenes -> Predefined and click on BodyPose to open the scene we worked on.
4. Click on play!

https://user-images.githubusercontent.com/46021351/208218367-edbbf2aa-02db-4649-9f5f-07ec74a78c6a.mp4

## Our Contributions can be found in
1. DaiBodyPose.cs
2. Body Pose Scene

As we mentioned above, this project is built on top of an existing work. To access the script we worked on, go to Assets -> Plugins -> OakForUnity -> Scripts -> Predefined -> Body Pose -> DaiBodyPose.cs.
Aside from the script, most of the components in the Body Pose scene were created or placed by us.
