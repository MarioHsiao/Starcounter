Model Operations
========

Adds sugar function(-s), to model operations on element's model easier.

### Installation

You can install it using [bower](http://bower.io/) `bower install Polyjuice/model-operations` or just download from [github](https://github.com/Polyjuice/model-operations).

Then add source to your head:

```html
<!-- include Polymer with dependencies -->
<script src="bower_components/platform/platform.js"></script>
<!-- include model-operations with dependencies -->
<link rel="import" href="bower_components/model-operations/model-operations.html"></script>
```

### Usage


```html
<button onclick="getModel(this).user.name = 'Scrappy'">Change to Scrappy</button>
```

### Changelog

#### 0.0.1 (Apr 2, 2014)

First version separated  from [PuppetJs](https://github.com/PuppetJs/PuppetJs)(#7c9845d)[https://github.com/PuppetJs/PuppetJs/tree/7c9845de0a317243320873d26978e35280746ff6]