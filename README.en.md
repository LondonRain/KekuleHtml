# Introduction

<p align="center">
  <img src="img/KekuleHtmlBanner.png" />
</p>

[![en](https://img.shields.io/badge/lang-en-red.svg)](README.en.md)
[![de](https://img.shields.io/badge/lang-de-green.svg)](README.md)

1. [Description](#description)
2. [Prerequisites](#prerequisites)
3. [Usage & Interface](#usage)
4. [Features](#features)
5. [Dependencies & License](#dependencies)

## Description

**KekuleHtml** was created to analyze personal genealogy data.

Based on a [GEDCOM](https://en.wikipedia.org/wiki/GEDCOM) file, it generates a compact HTML family tree according to [Kekule](https://en.wikipedia.org/wiki/Ahnentafel), which can help, for example, with navigating your own data and the directory structures of your sources.

In addition, it generates generational statistics and—provided geodata is available—a map that allows you to visualize migration patterns across different generations and family branches.

## Requirements

- > ℹ️ The application is based on the [.NET 10 Framework](https://dotnet.microsoft.com/en-us/download/dotnet/10.0) . This must be installed. 
- To ensure that date values are imported correctly, they must comply with the [GEDCOM standard](https://gedcom.io/specifications/FamilySearchGEDCOMv7.html#date). For example, German month names do not work.
- For the map functionality, locations must have been georeferenced. The program does not perform its own georeferencing based on place names. See the section [“Geographic Distribution of Ancestral Lines”](#geographic-distribution-of-ancestral-lines).

## Usage

You must specify your own GEDCOM file as a command-line parameter:
`KekuleHtml.exe kennedy.ged`

An alphabetically sorted list of all individuals in the file is displayed. The starting person (Kekule number 0) must be selected by entering their preceding number.

![Select Starting Person](img/select_start_person.png)

A `kekule.html` file is then created in the same directory as the GEDCOM file. This file does not open automatically in the browser.

### User Interface

There is also a user interface. To use it, run `KekuleHtmlUi.exe`.

![User Interface](img/ui.png)

You can select a GEDCOM file using “Browse...”. Alternatively, you can drag and drop the file (e.g., from Windows Explorer) onto the window to open it. Under “Select Starting Person...”, you can open a drop-down list to specify the starting person. Typing in the text field filters the drop-down list accordingly. Under “Generations” you can limit the output to a specified number of generations. You can use the checkbox to specify whether the file generated after clicking “Create HTML File” should open directly.

### Parameters

Both the console application and the application with a graphical user interface support the following command-line parameters.

| Parameter (with example) | Description |
| ------------------------ | ------------ |
| kennedy.ged | A relative or absolute path to a GEDCOM file. This is then loaded directly in the user interface. However, it is usually more convenient to select the file yourself via the user interface. |
| -maxGenerations 12 | Number of generations included in the output. 20 is the default value if this parameter is not set. The maximum is 63. |
| -lang en | Forces the specified language. Currently, `de` (German) and `en` (English) are supported. If no language is specified via a parameter or if the language is unknown, the system language is used. |

### Ahnenblatt

For integration with the [Ahnenblatt](https://www.ahnenblatt.de/) software, see [here](AhnenblattPlugin/ReadmeAB.md).

## Features

### Table of Contents & Chronological Order of Generations

A table of contents with links to each generation is displayed.

![Table of Contents & Chronological Order of Generations](img/toc.png)

In addition, a timeline chart is generated that displays the lifespan of each generation. The gray bars extend from the earliest birth date to the latest death date. If not every person in the generation has a death date recorded and at least one of the birth years occurred 110 years ago or less, it is assumed that the generation is still alive, and the bar extends into the current year. The blue bars represent the respective time span between the median birth and death years. The bars are also clickable and will take you to the list for the respective generation.

### Geographic Distribution of Ancestral Lines

If geolocated places are available for individuals ([LATI and LONG tags](https://gedcom.io/specifications/FamilySearchGEDCOMv7.html#latitude) in GEDCOM), a map is displayed.

![Map of the geographic distribution of ancestral lines](img/migration_map.png)

#### Data Basis

As the basis for the map, the following events are collected for each person, including name, date, family branch, and location—if a location with coordinates is available:
- Place of birth
- Place of marriage
- Place of death
- Places of residence for an individual
- Places of residence for a marriage/family

#### Clusters

These points are collected and grouped into clusters by location and family branch. The color coding of the respective family lines, based on [Mary Hill](http://www.genrootsorganizer.com/p/13-steps.html), is used starting from the four grandparents.

Each colored point on the map represents one of these clusters. The larger the circle, the more events have taken place at that location. The lighter the circle, the older the first event that occurred there; the darker the circle, the more recent the event. The shading based on event age is relative to each of the four family branches.

If event clusters from multiple family branches exist at a single location, they do not overlap but are displayed slightly offset from one another, as shown in the following screenshot.

![Cluster with Offset](img/migration_map_offset.png)

Clicking on one of the clusters opens a pop-up showing the place name, the number of events, and the time period of the events. Under “Details,” you’ll find an alphabetically sorted list of the people associated with that location, including their birth (*), death (✝), and marriage (⚭) years, as well as the time period during which they lived there (⌂).

![Cluster Details](img/migration_map_details.png)

### Kekule List
In the actual ancestor list, all individuals for each generation are displayed in a compact format. Here, too, Mary Hill’s color-coding system is used.

For each generation, the time period covering births and deaths is displayed, and the average and median lifespans for that generation are calculated.

![Ancestor List](img/kekule.png)

## Dependencies

- **KekuleHtml** uses [GeneGenie.Gedcom](https://github.com/TheGeneGenieProject/GeneGenie.Gedcom) to parse the GEDCOM file.
- [Leaflet](https://leafletjs.com/) is used with [OpenStreetMap.de](https://www.openstreetmap.de/) data for map rendering.
- [Leaflet Control FullScreen](https://github.com/brunob/leaflet.fullscreen) adds a full-screen button for the map.
- The [Kennedy Family](https://github.com/D-Jeffrey/gedcom-samples/tree/main#kennedy) sample GEDCOM file from [gedcom-samples](https://github.com/D-Jeffrey/gedcom-samples) was used to create the documentation.

Many thanks to everyone!

### License

Like [GeneGenie.Gedcom](https://github.com/TheGeneGenieProject/GeneGenie.Gedcom), **KekuleHtml** is licensed under the [AGPL 3.0](licence.txt).