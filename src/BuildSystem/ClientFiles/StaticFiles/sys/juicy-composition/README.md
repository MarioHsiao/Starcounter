# &lt;juicy-composition&gt; [![Build Status](https://travis-ci.org/Juicy/juicy-composition.svg?branch=gh-pages)](https://travis-ci.org/Juicy/juicy-composition)

> Custom Element that adds given Document Fragment to Shadow DOM

## Demo

[Check it live!](http://Juicy.github.io/juicy-composition)

## Install

Install the component using [Bower](http://bower.io/):

```sh
$ bower install juicy-composition --save
```

Or [download as ZIP](https://github.com/Juicy/juicy-composition/archive/gh-pages.zip).

## Usage

1. Import polyfill, if needed:

    ```html
    <script src="bower_components/webcomponentsjs/webcomponents.min.js"></script>
    ```

2. Import custom element:

    ```html
    <link rel="import" href="bower_components/juicy-composition/juicy-composition.html">
    ```

3. Start using it!

    ```html
    <template id="my-shadow">
        <h1>Here goes</h1>
        <p>Any HTML structure to be put into `juicy-composition`s shadowRoot</p>
        <p>It may contain slots: <content select="[slot='my-slot']"></content></p>
    </template>
    ....
    <juicy-composition>
        <div>My thing</div>
    </juicy-composition>
    <script>
        var juicyComposition = document.querySelector('juicy-composition');
        juicyComposition.composition = document.importNode(document.querySelector('#my-shadow').content, true);
        juicyComposition.stamp();
    </script>
    ```

## Attributes

Attribute     | Options     | Default      | Description
---           | ---         | ---          | ---
`auto-stamp`  | *Boolean*   | `false`      | Set to make it stamp Shadow DOM on created and every composition change. It's also a `autoStamp` property. If set in run-time stamps imediately.

## Properties

Attribute     | Options            | Default | Description
---           | ---                | ---     | ---
`composition` | *DocumentFragment* |         | Document Fragment to be used to in element's shadowRoot
`autoStamp`   | *Boolean*          | `false` | See [#Attributes]

## Methods

Method        | Parameters   | Returns     | Description
---           | ---          | ---         | ---
`stamp`       |              |             | Call it to imperatively stamp shadow DOM tree. If `auto-stamp` attribute is set, it's done automatically, when element is created, or composition is changed.

## Events

Event     | Description
---       | ---
`stamped` | Triggered once shadow DOM is stamped

## Slots

In your composition you can/should use slots to distribute the content within your layout. Conceptually, it matches Shadow DOM V1, however, we still support V0 syntax, due to browser & polyfill coverage.

`<juicy-composition>` adds also few handy features on top of that.

### Custom slots
Naturally, thanks to Shadow DOM. If you can/want, you setup everything explicitly

```html
<juicy-composition>
  <div slot="my-slot-name">smth</div>
</juicy-composition>
```
```html
<template id="composition">
  My composition structure
  ...
  <content select="[slot='my-slot-name']"></content>
  <!-- in Shadow DOM V1 it will look like: -->
  <slot name="my-slot-name"></slot>
</template>
```

### Automatic slot names
If for some reason your content does not have slot names, `juicy-composition` will add it automatically,
so you will be able to distribute it easily, even if your content provider cannot do so.
We will use child number as a slot-name.

```html
<juicy-composition>
  <div slot="0">...</div><!-- slot name automatically generated -->
  <div slot="provided-name">...</div>
  <div slot="2">...</div><!-- slot name automatically generated -->
</juicy-composition>
```

#### Scoped slot names

You can also add name-space to your slot names, if for example you concatenate the content from many providers

```html
<juicy-composition>
  <meta itemprop="juicy-composition-scope" content="fruits"></meta>
    <div slot="fruits-provider-slot-name">apple</div>
    <div>plum</div>
  <meta itemprop="juicy-composition-scope" content="veggies"></meta>
    <div>carrot</div>
</juicy-composition>
```
Will make slot names look like this
```html
<juicy-composition>
    <meta itemprop="juicy-composition-scope" content="fruits"></meta>
    <div slot="fruits-provider-slot-name">apple</div>
    <div slot="fruits/1">plum</div>
    <meta itemprop="juicy-composition-scope" content="veggies"></meta>
    <div slot="veggies/0">carrot</div>
</juicy-composition>
```

### Automatic slots
In case some content is added dynamically, out you just missed to add some slots for given content we will create slots automatically as well.
We will append new `<slot>` (`<content>`) element(-s) to the end of the composition.

## [Contributing and Development](CONTRIBUTING.md)

## History

For detailed changelog, check [Releases](https://github.com/Juicy/juicy-composition/releases).

## License

[MIT License](http://opensource.org/licenses/MIT)
