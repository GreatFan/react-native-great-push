'use strict';

var pkg = require('./package');
var React = require('react-native');
var {
  AppRegistry,
  StyleSheet,
  Text,
  View,
} = React;

var GreatPush = require('react-native-great-push');

var UpdateOnStart = React.createClass({
  componentDidMount: function() {
    GreatPush.checkForUpdate().done((update) => {
      if (update && update.downloadUrl) {
        update.download().done((newPackage) => {
          newPackage.install();
        });
      }
    });
  },
  render: function() {
    return (
      <View style={styles.container}>
        <Text>
          Welcome to {pkg.name} {pkg.version}!
        </Text>
      </View>
    );
  }
});

var styles = StyleSheet.create({
  container: {
    flex: 1,
    justifyContent: 'center',
    alignItems: 'center',
    backgroundColor: '#F5FCFF',
  }
});

AppRegistry.registerComponent('UpdateOnStart', () => UpdateOnStart);
