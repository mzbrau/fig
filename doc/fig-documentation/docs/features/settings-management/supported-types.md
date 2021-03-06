---
sidebar_position: 1
---

# Supported Types

Fig supports most c# property types out of the box with customized editors for each type. More complex types are supported using data grids or JSON.

Some of the settings such as strings and ints also support other capabilities such as **valid value dropdown** or **validation**.

## String

Text based settings.

***Note**: If nullable settings are enabled, strings must either be nullable or have a default value.*

![string-setting](../../../static/img/string-setting.png)

## Bool

True or false values.

![bool-setting](../../../static/img/bool-setting.png)

## Double

Decimal numbers.

![double-setting](../../../static/img/double-setting.png)

## Int

Whole numbers.

![image-20220726225609084](../../../static/img/int-setting.png)

## Long

Larger whole numbers.

![image-20220726225708951](../../../static/img/long-setting.png)

## DateTime

Date and Time

![2022-07-26 23.32.36](../../../static/img/date-time-setting.png)

## TimeSpan

A time range.

***Note:** This is represented as a string within the web interface.*

![image-20220726230046584](../../../static/img/image-20220726230046584.png)

## Data Grid

Data grids can be used to group multiple values in a collection. For example a data grid can be used for list of strings or integers or it could be a collection of objects where the class definition for the object contains multiple properties of different types.

![image-20220726230140744](../../../static/img/data-grid-setting.png)

## JSON

JSON representation of a class. This is the fallback mode for Fig if it cannot find another match for this property type.

![2022-07-26 23.02.28](../../../static/img/json-setting.png)

