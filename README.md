# visualanomalydetection
The following YouTube video demonstrates that it is possible to use a camera to predict malfunctions in a factory or machine without having the need to attach vibration sensors:
https://www.youtube.com/watch?v=rEoc0YoALt0

The purpose of this project is to create a PoC to test the feasibility of having a simple Raspberry PI with an embedded camera watch over a toy factory setup and detect vibrations invisible to the naked eye.

The solution takes several pictures per second and a diff-algorithm calculates pixel differences between the adjacent pictures.
The diff value and a timestamp can be sent to the Azure Anomaly Detector.
The detector should be able to visualize or trigger an alarm when an anomaly is found.
