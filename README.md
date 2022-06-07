# CIROS-file-to-MQTT-Publisher

Student thesis in the 6th theoretical semester of my studies in electrical engineering - automation with [Prof. Dr. Bozena Lamek-Creutz, DHBW](https://www.linkedin.com/in/dr-ing-bozena-lamek-creutz-943766105)

This script interfaces with two Mitsubishi robots, an RV-3SDB an an RH-6SDH5520, that are part of a Festo modular production system located at [DHBW Mannheim](https://www.mannheim.dhbw.de/).
It reads the robot data directly from TCP-packets using [SharpPcap](https://github.com/dotpcap/sharppcap) and publishes the data to an MQTT-broker using [MQTTnet](https://github.com/dotnet/MQTTnet).

[Link to the documentation (German)](https://docs.google.com/document/d/1aoB_pFMlUvaGXtW5EZLKQ5L99xn3KzHSzCvQDiuA1Uk).

Robot positions on the MQTT-broker visualized using [MQTT Explorer](https://mqtt-explorer.com). J1 is plotted over time, indicating that the robot performed a repeated motion with a break during that timeframe:

![MQTT Explorer MOVING NOT MOVING published positions](https://user-images.githubusercontent.com/70020564/172181235-3e573d67-2956-4d0b-a75e-a40fb8ceff73.PNG)
