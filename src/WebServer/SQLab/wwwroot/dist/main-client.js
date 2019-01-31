/******/ (function(modules) { // webpackBootstrap
/******/ 	function hotDisposeChunk(chunkId) {
/******/ 		delete installedChunks[chunkId];
/******/ 	}
/******/ 	var parentHotUpdateCallback = this["webpackHotUpdate"];
/******/ 	this["webpackHotUpdate"] = 
/******/ 	function webpackHotUpdateCallback(chunkId, moreModules) { // eslint-disable-line no-unused-vars
/******/ 		hotAddUpdateChunk(chunkId, moreModules);
/******/ 		if(parentHotUpdateCallback) parentHotUpdateCallback(chunkId, moreModules);
/******/ 	} ;
/******/ 	
/******/ 	function hotDownloadUpdateChunk(chunkId) { // eslint-disable-line no-unused-vars
/******/ 		var head = document.getElementsByTagName("head")[0];
/******/ 		var script = document.createElement("script");
/******/ 		script.type = "text/javascript";
/******/ 		script.charset = "utf-8";
/******/ 		script.src = __webpack_require__.p + "" + chunkId + "." + hotCurrentHash + ".hot-update.js";
/******/ 		head.appendChild(script);
/******/ 	}
/******/ 	
/******/ 	function hotDownloadManifest() { // eslint-disable-line no-unused-vars
/******/ 		return new Promise(function(resolve, reject) {
/******/ 			if(typeof XMLHttpRequest === "undefined")
/******/ 				return reject(new Error("No browser support"));
/******/ 			try {
/******/ 				var request = new XMLHttpRequest();
/******/ 				var requestPath = __webpack_require__.p + "" + hotCurrentHash + ".hot-update.json";
/******/ 				request.open("GET", requestPath, true);
/******/ 				request.timeout = 10000;
/******/ 				request.send(null);
/******/ 			} catch(err) {
/******/ 				return reject(err);
/******/ 			}
/******/ 			request.onreadystatechange = function() {
/******/ 				if(request.readyState !== 4) return;
/******/ 				if(request.status === 0) {
/******/ 					// timeout
/******/ 					reject(new Error("Manifest request to " + requestPath + " timed out."));
/******/ 				} else if(request.status === 404) {
/******/ 					// no update available
/******/ 					resolve();
/******/ 				} else if(request.status !== 200 && request.status !== 304) {
/******/ 					// other failure
/******/ 					reject(new Error("Manifest request to " + requestPath + " failed."));
/******/ 				} else {
/******/ 					// success
/******/ 					try {
/******/ 						var update = JSON.parse(request.responseText);
/******/ 					} catch(e) {
/******/ 						reject(e);
/******/ 						return;
/******/ 					}
/******/ 					resolve(update);
/******/ 				}
/******/ 			};
/******/ 		});
/******/ 	}
/******/
/******/ 	
/******/ 	
/******/ 	var hotApplyOnUpdate = true;
/******/ 	var hotCurrentHash = "a0c6dacd433eaba36335"; // eslint-disable-line no-unused-vars
/******/ 	var hotCurrentModuleData = {};
/******/ 	var hotCurrentChildModule; // eslint-disable-line no-unused-vars
/******/ 	var hotCurrentParents = []; // eslint-disable-line no-unused-vars
/******/ 	var hotCurrentParentsTemp = []; // eslint-disable-line no-unused-vars
/******/ 	
/******/ 	function hotCreateRequire(moduleId) { // eslint-disable-line no-unused-vars
/******/ 		var me = installedModules[moduleId];
/******/ 		if(!me) return __webpack_require__;
/******/ 		var fn = function(request) {
/******/ 			if(me.hot.active) {
/******/ 				if(installedModules[request]) {
/******/ 					if(installedModules[request].parents.indexOf(moduleId) < 0)
/******/ 						installedModules[request].parents.push(moduleId);
/******/ 				} else {
/******/ 					hotCurrentParents = [moduleId];
/******/ 					hotCurrentChildModule = request;
/******/ 				}
/******/ 				if(me.children.indexOf(request) < 0)
/******/ 					me.children.push(request);
/******/ 			} else {
/******/ 				console.warn("[HMR] unexpected require(" + request + ") from disposed module " + moduleId);
/******/ 				hotCurrentParents = [];
/******/ 			}
/******/ 			return __webpack_require__(request);
/******/ 		};
/******/ 		var ObjectFactory = function ObjectFactory(name) {
/******/ 			return {
/******/ 				configurable: true,
/******/ 				enumerable: true,
/******/ 				get: function() {
/******/ 					return __webpack_require__[name];
/******/ 				},
/******/ 				set: function(value) {
/******/ 					__webpack_require__[name] = value;
/******/ 				}
/******/ 			};
/******/ 		};
/******/ 		for(var name in __webpack_require__) {
/******/ 			if(Object.prototype.hasOwnProperty.call(__webpack_require__, name) && name !== "e") {
/******/ 				Object.defineProperty(fn, name, ObjectFactory(name));
/******/ 			}
/******/ 		}
/******/ 		fn.e = function(chunkId) {
/******/ 			if(hotStatus === "ready")
/******/ 				hotSetStatus("prepare");
/******/ 			hotChunksLoading++;
/******/ 			return __webpack_require__.e(chunkId).then(finishChunkLoading, function(err) {
/******/ 				finishChunkLoading();
/******/ 				throw err;
/******/ 			});
/******/ 	
/******/ 			function finishChunkLoading() {
/******/ 				hotChunksLoading--;
/******/ 				if(hotStatus === "prepare") {
/******/ 					if(!hotWaitingFilesMap[chunkId]) {
/******/ 						hotEnsureUpdateChunk(chunkId);
/******/ 					}
/******/ 					if(hotChunksLoading === 0 && hotWaitingFiles === 0) {
/******/ 						hotUpdateDownloaded();
/******/ 					}
/******/ 				}
/******/ 			}
/******/ 		};
/******/ 		return fn;
/******/ 	}
/******/ 	
/******/ 	function hotCreateModule(moduleId) { // eslint-disable-line no-unused-vars
/******/ 		var hot = {
/******/ 			// private stuff
/******/ 			_acceptedDependencies: {},
/******/ 			_declinedDependencies: {},
/******/ 			_selfAccepted: false,
/******/ 			_selfDeclined: false,
/******/ 			_disposeHandlers: [],
/******/ 			_main: hotCurrentChildModule !== moduleId,
/******/ 	
/******/ 			// Module API
/******/ 			active: true,
/******/ 			accept: function(dep, callback) {
/******/ 				if(typeof dep === "undefined")
/******/ 					hot._selfAccepted = true;
/******/ 				else if(typeof dep === "function")
/******/ 					hot._selfAccepted = dep;
/******/ 				else if(typeof dep === "object")
/******/ 					for(var i = 0; i < dep.length; i++)
/******/ 						hot._acceptedDependencies[dep[i]] = callback || function() {};
/******/ 				else
/******/ 					hot._acceptedDependencies[dep] = callback || function() {};
/******/ 			},
/******/ 			decline: function(dep) {
/******/ 				if(typeof dep === "undefined")
/******/ 					hot._selfDeclined = true;
/******/ 				else if(typeof dep === "object")
/******/ 					for(var i = 0; i < dep.length; i++)
/******/ 						hot._declinedDependencies[dep[i]] = true;
/******/ 				else
/******/ 					hot._declinedDependencies[dep] = true;
/******/ 			},
/******/ 			dispose: function(callback) {
/******/ 				hot._disposeHandlers.push(callback);
/******/ 			},
/******/ 			addDisposeHandler: function(callback) {
/******/ 				hot._disposeHandlers.push(callback);
/******/ 			},
/******/ 			removeDisposeHandler: function(callback) {
/******/ 				var idx = hot._disposeHandlers.indexOf(callback);
/******/ 				if(idx >= 0) hot._disposeHandlers.splice(idx, 1);
/******/ 			},
/******/ 	
/******/ 			// Management API
/******/ 			check: hotCheck,
/******/ 			apply: hotApply,
/******/ 			status: function(l) {
/******/ 				if(!l) return hotStatus;
/******/ 				hotStatusHandlers.push(l);
/******/ 			},
/******/ 			addStatusHandler: function(l) {
/******/ 				hotStatusHandlers.push(l);
/******/ 			},
/******/ 			removeStatusHandler: function(l) {
/******/ 				var idx = hotStatusHandlers.indexOf(l);
/******/ 				if(idx >= 0) hotStatusHandlers.splice(idx, 1);
/******/ 			},
/******/ 	
/******/ 			//inherit from previous dispose call
/******/ 			data: hotCurrentModuleData[moduleId]
/******/ 		};
/******/ 		hotCurrentChildModule = undefined;
/******/ 		return hot;
/******/ 	}
/******/ 	
/******/ 	var hotStatusHandlers = [];
/******/ 	var hotStatus = "idle";
/******/ 	
/******/ 	function hotSetStatus(newStatus) {
/******/ 		hotStatus = newStatus;
/******/ 		for(var i = 0; i < hotStatusHandlers.length; i++)
/******/ 			hotStatusHandlers[i].call(null, newStatus);
/******/ 	}
/******/ 	
/******/ 	// while downloading
/******/ 	var hotWaitingFiles = 0;
/******/ 	var hotChunksLoading = 0;
/******/ 	var hotWaitingFilesMap = {};
/******/ 	var hotRequestedFilesMap = {};
/******/ 	var hotAvailableFilesMap = {};
/******/ 	var hotDeferred;
/******/ 	
/******/ 	// The update info
/******/ 	var hotUpdate, hotUpdateNewHash;
/******/ 	
/******/ 	function toModuleId(id) {
/******/ 		var isNumber = (+id) + "" === id;
/******/ 		return isNumber ? +id : id;
/******/ 	}
/******/ 	
/******/ 	function hotCheck(apply) {
/******/ 		if(hotStatus !== "idle") throw new Error("check() is only allowed in idle status");
/******/ 		hotApplyOnUpdate = apply;
/******/ 		hotSetStatus("check");
/******/ 		return hotDownloadManifest().then(function(update) {
/******/ 			if(!update) {
/******/ 				hotSetStatus("idle");
/******/ 				return null;
/******/ 			}
/******/ 			hotRequestedFilesMap = {};
/******/ 			hotWaitingFilesMap = {};
/******/ 			hotAvailableFilesMap = update.c;
/******/ 			hotUpdateNewHash = update.h;
/******/ 	
/******/ 			hotSetStatus("prepare");
/******/ 			var promise = new Promise(function(resolve, reject) {
/******/ 				hotDeferred = {
/******/ 					resolve: resolve,
/******/ 					reject: reject
/******/ 				};
/******/ 			});
/******/ 			hotUpdate = {};
/******/ 			var chunkId = 0;
/******/ 			{ // eslint-disable-line no-lone-blocks
/******/ 				/*globals chunkId */
/******/ 				hotEnsureUpdateChunk(chunkId);
/******/ 			}
/******/ 			if(hotStatus === "prepare" && hotChunksLoading === 0 && hotWaitingFiles === 0) {
/******/ 				hotUpdateDownloaded();
/******/ 			}
/******/ 			return promise;
/******/ 		});
/******/ 	}
/******/ 	
/******/ 	function hotAddUpdateChunk(chunkId, moreModules) { // eslint-disable-line no-unused-vars
/******/ 		if(!hotAvailableFilesMap[chunkId] || !hotRequestedFilesMap[chunkId])
/******/ 			return;
/******/ 		hotRequestedFilesMap[chunkId] = false;
/******/ 		for(var moduleId in moreModules) {
/******/ 			if(Object.prototype.hasOwnProperty.call(moreModules, moduleId)) {
/******/ 				hotUpdate[moduleId] = moreModules[moduleId];
/******/ 			}
/******/ 		}
/******/ 		if(--hotWaitingFiles === 0 && hotChunksLoading === 0) {
/******/ 			hotUpdateDownloaded();
/******/ 		}
/******/ 	}
/******/ 	
/******/ 	function hotEnsureUpdateChunk(chunkId) {
/******/ 		if(!hotAvailableFilesMap[chunkId]) {
/******/ 			hotWaitingFilesMap[chunkId] = true;
/******/ 		} else {
/******/ 			hotRequestedFilesMap[chunkId] = true;
/******/ 			hotWaitingFiles++;
/******/ 			hotDownloadUpdateChunk(chunkId);
/******/ 		}
/******/ 	}
/******/ 	
/******/ 	function hotUpdateDownloaded() {
/******/ 		hotSetStatus("ready");
/******/ 		var deferred = hotDeferred;
/******/ 		hotDeferred = null;
/******/ 		if(!deferred) return;
/******/ 		if(hotApplyOnUpdate) {
/******/ 			hotApply(hotApplyOnUpdate).then(function(result) {
/******/ 				deferred.resolve(result);
/******/ 			}, function(err) {
/******/ 				deferred.reject(err);
/******/ 			});
/******/ 		} else {
/******/ 			var outdatedModules = [];
/******/ 			for(var id in hotUpdate) {
/******/ 				if(Object.prototype.hasOwnProperty.call(hotUpdate, id)) {
/******/ 					outdatedModules.push(toModuleId(id));
/******/ 				}
/******/ 			}
/******/ 			deferred.resolve(outdatedModules);
/******/ 		}
/******/ 	}
/******/ 	
/******/ 	function hotApply(options) {
/******/ 		if(hotStatus !== "ready") throw new Error("apply() is only allowed in ready status");
/******/ 		options = options || {};
/******/ 	
/******/ 		var cb;
/******/ 		var i;
/******/ 		var j;
/******/ 		var module;
/******/ 		var moduleId;
/******/ 	
/******/ 		function getAffectedStuff(updateModuleId) {
/******/ 			var outdatedModules = [updateModuleId];
/******/ 			var outdatedDependencies = {};
/******/ 	
/******/ 			var queue = outdatedModules.slice().map(function(id) {
/******/ 				return {
/******/ 					chain: [id],
/******/ 					id: id
/******/ 				};
/******/ 			});
/******/ 			while(queue.length > 0) {
/******/ 				var queueItem = queue.pop();
/******/ 				var moduleId = queueItem.id;
/******/ 				var chain = queueItem.chain;
/******/ 				module = installedModules[moduleId];
/******/ 				if(!module || module.hot._selfAccepted)
/******/ 					continue;
/******/ 				if(module.hot._selfDeclined) {
/******/ 					return {
/******/ 						type: "self-declined",
/******/ 						chain: chain,
/******/ 						moduleId: moduleId
/******/ 					};
/******/ 				}
/******/ 				if(module.hot._main) {
/******/ 					return {
/******/ 						type: "unaccepted",
/******/ 						chain: chain,
/******/ 						moduleId: moduleId
/******/ 					};
/******/ 				}
/******/ 				for(var i = 0; i < module.parents.length; i++) {
/******/ 					var parentId = module.parents[i];
/******/ 					var parent = installedModules[parentId];
/******/ 					if(!parent) continue;
/******/ 					if(parent.hot._declinedDependencies[moduleId]) {
/******/ 						return {
/******/ 							type: "declined",
/******/ 							chain: chain.concat([parentId]),
/******/ 							moduleId: moduleId,
/******/ 							parentId: parentId
/******/ 						};
/******/ 					}
/******/ 					if(outdatedModules.indexOf(parentId) >= 0) continue;
/******/ 					if(parent.hot._acceptedDependencies[moduleId]) {
/******/ 						if(!outdatedDependencies[parentId])
/******/ 							outdatedDependencies[parentId] = [];
/******/ 						addAllToSet(outdatedDependencies[parentId], [moduleId]);
/******/ 						continue;
/******/ 					}
/******/ 					delete outdatedDependencies[parentId];
/******/ 					outdatedModules.push(parentId);
/******/ 					queue.push({
/******/ 						chain: chain.concat([parentId]),
/******/ 						id: parentId
/******/ 					});
/******/ 				}
/******/ 			}
/******/ 	
/******/ 			return {
/******/ 				type: "accepted",
/******/ 				moduleId: updateModuleId,
/******/ 				outdatedModules: outdatedModules,
/******/ 				outdatedDependencies: outdatedDependencies
/******/ 			};
/******/ 		}
/******/ 	
/******/ 		function addAllToSet(a, b) {
/******/ 			for(var i = 0; i < b.length; i++) {
/******/ 				var item = b[i];
/******/ 				if(a.indexOf(item) < 0)
/******/ 					a.push(item);
/******/ 			}
/******/ 		}
/******/ 	
/******/ 		// at begin all updates modules are outdated
/******/ 		// the "outdated" status can propagate to parents if they don't accept the children
/******/ 		var outdatedDependencies = {};
/******/ 		var outdatedModules = [];
/******/ 		var appliedUpdate = {};
/******/ 	
/******/ 		var warnUnexpectedRequire = function warnUnexpectedRequire() {
/******/ 			console.warn("[HMR] unexpected require(" + result.moduleId + ") to disposed module");
/******/ 		};
/******/ 	
/******/ 		for(var id in hotUpdate) {
/******/ 			if(Object.prototype.hasOwnProperty.call(hotUpdate, id)) {
/******/ 				moduleId = toModuleId(id);
/******/ 				var result;
/******/ 				if(hotUpdate[id]) {
/******/ 					result = getAffectedStuff(moduleId);
/******/ 				} else {
/******/ 					result = {
/******/ 						type: "disposed",
/******/ 						moduleId: id
/******/ 					};
/******/ 				}
/******/ 				var abortError = false;
/******/ 				var doApply = false;
/******/ 				var doDispose = false;
/******/ 				var chainInfo = "";
/******/ 				if(result.chain) {
/******/ 					chainInfo = "\nUpdate propagation: " + result.chain.join(" -> ");
/******/ 				}
/******/ 				switch(result.type) {
/******/ 					case "self-declined":
/******/ 						if(options.onDeclined)
/******/ 							options.onDeclined(result);
/******/ 						if(!options.ignoreDeclined)
/******/ 							abortError = new Error("Aborted because of self decline: " + result.moduleId + chainInfo);
/******/ 						break;
/******/ 					case "declined":
/******/ 						if(options.onDeclined)
/******/ 							options.onDeclined(result);
/******/ 						if(!options.ignoreDeclined)
/******/ 							abortError = new Error("Aborted because of declined dependency: " + result.moduleId + " in " + result.parentId + chainInfo);
/******/ 						break;
/******/ 					case "unaccepted":
/******/ 						if(options.onUnaccepted)
/******/ 							options.onUnaccepted(result);
/******/ 						if(!options.ignoreUnaccepted)
/******/ 							abortError = new Error("Aborted because " + moduleId + " is not accepted" + chainInfo);
/******/ 						break;
/******/ 					case "accepted":
/******/ 						if(options.onAccepted)
/******/ 							options.onAccepted(result);
/******/ 						doApply = true;
/******/ 						break;
/******/ 					case "disposed":
/******/ 						if(options.onDisposed)
/******/ 							options.onDisposed(result);
/******/ 						doDispose = true;
/******/ 						break;
/******/ 					default:
/******/ 						throw new Error("Unexception type " + result.type);
/******/ 				}
/******/ 				if(abortError) {
/******/ 					hotSetStatus("abort");
/******/ 					return Promise.reject(abortError);
/******/ 				}
/******/ 				if(doApply) {
/******/ 					appliedUpdate[moduleId] = hotUpdate[moduleId];
/******/ 					addAllToSet(outdatedModules, result.outdatedModules);
/******/ 					for(moduleId in result.outdatedDependencies) {
/******/ 						if(Object.prototype.hasOwnProperty.call(result.outdatedDependencies, moduleId)) {
/******/ 							if(!outdatedDependencies[moduleId])
/******/ 								outdatedDependencies[moduleId] = [];
/******/ 							addAllToSet(outdatedDependencies[moduleId], result.outdatedDependencies[moduleId]);
/******/ 						}
/******/ 					}
/******/ 				}
/******/ 				if(doDispose) {
/******/ 					addAllToSet(outdatedModules, [result.moduleId]);
/******/ 					appliedUpdate[moduleId] = warnUnexpectedRequire;
/******/ 				}
/******/ 			}
/******/ 		}
/******/ 	
/******/ 		// Store self accepted outdated modules to require them later by the module system
/******/ 		var outdatedSelfAcceptedModules = [];
/******/ 		for(i = 0; i < outdatedModules.length; i++) {
/******/ 			moduleId = outdatedModules[i];
/******/ 			if(installedModules[moduleId] && installedModules[moduleId].hot._selfAccepted)
/******/ 				outdatedSelfAcceptedModules.push({
/******/ 					module: moduleId,
/******/ 					errorHandler: installedModules[moduleId].hot._selfAccepted
/******/ 				});
/******/ 		}
/******/ 	
/******/ 		// Now in "dispose" phase
/******/ 		hotSetStatus("dispose");
/******/ 		Object.keys(hotAvailableFilesMap).forEach(function(chunkId) {
/******/ 			if(hotAvailableFilesMap[chunkId] === false) {
/******/ 				hotDisposeChunk(chunkId);
/******/ 			}
/******/ 		});
/******/ 	
/******/ 		var idx;
/******/ 		var queue = outdatedModules.slice();
/******/ 		while(queue.length > 0) {
/******/ 			moduleId = queue.pop();
/******/ 			module = installedModules[moduleId];
/******/ 			if(!module) continue;
/******/ 	
/******/ 			var data = {};
/******/ 	
/******/ 			// Call dispose handlers
/******/ 			var disposeHandlers = module.hot._disposeHandlers;
/******/ 			for(j = 0; j < disposeHandlers.length; j++) {
/******/ 				cb = disposeHandlers[j];
/******/ 				cb(data);
/******/ 			}
/******/ 			hotCurrentModuleData[moduleId] = data;
/******/ 	
/******/ 			// disable module (this disables requires from this module)
/******/ 			module.hot.active = false;
/******/ 	
/******/ 			// remove module from cache
/******/ 			delete installedModules[moduleId];
/******/ 	
/******/ 			// remove "parents" references from all children
/******/ 			for(j = 0; j < module.children.length; j++) {
/******/ 				var child = installedModules[module.children[j]];
/******/ 				if(!child) continue;
/******/ 				idx = child.parents.indexOf(moduleId);
/******/ 				if(idx >= 0) {
/******/ 					child.parents.splice(idx, 1);
/******/ 				}
/******/ 			}
/******/ 		}
/******/ 	
/******/ 		// remove outdated dependency from module children
/******/ 		var dependency;
/******/ 		var moduleOutdatedDependencies;
/******/ 		for(moduleId in outdatedDependencies) {
/******/ 			if(Object.prototype.hasOwnProperty.call(outdatedDependencies, moduleId)) {
/******/ 				module = installedModules[moduleId];
/******/ 				if(module) {
/******/ 					moduleOutdatedDependencies = outdatedDependencies[moduleId];
/******/ 					for(j = 0; j < moduleOutdatedDependencies.length; j++) {
/******/ 						dependency = moduleOutdatedDependencies[j];
/******/ 						idx = module.children.indexOf(dependency);
/******/ 						if(idx >= 0) module.children.splice(idx, 1);
/******/ 					}
/******/ 				}
/******/ 			}
/******/ 		}
/******/ 	
/******/ 		// Not in "apply" phase
/******/ 		hotSetStatus("apply");
/******/ 	
/******/ 		hotCurrentHash = hotUpdateNewHash;
/******/ 	
/******/ 		// insert new code
/******/ 		for(moduleId in appliedUpdate) {
/******/ 			if(Object.prototype.hasOwnProperty.call(appliedUpdate, moduleId)) {
/******/ 				modules[moduleId] = appliedUpdate[moduleId];
/******/ 			}
/******/ 		}
/******/ 	
/******/ 		// call accept handlers
/******/ 		var error = null;
/******/ 		for(moduleId in outdatedDependencies) {
/******/ 			if(Object.prototype.hasOwnProperty.call(outdatedDependencies, moduleId)) {
/******/ 				module = installedModules[moduleId];
/******/ 				moduleOutdatedDependencies = outdatedDependencies[moduleId];
/******/ 				var callbacks = [];
/******/ 				for(i = 0; i < moduleOutdatedDependencies.length; i++) {
/******/ 					dependency = moduleOutdatedDependencies[i];
/******/ 					cb = module.hot._acceptedDependencies[dependency];
/******/ 					if(callbacks.indexOf(cb) >= 0) continue;
/******/ 					callbacks.push(cb);
/******/ 				}
/******/ 				for(i = 0; i < callbacks.length; i++) {
/******/ 					cb = callbacks[i];
/******/ 					try {
/******/ 						cb(moduleOutdatedDependencies);
/******/ 					} catch(err) {
/******/ 						if(options.onErrored) {
/******/ 							options.onErrored({
/******/ 								type: "accept-errored",
/******/ 								moduleId: moduleId,
/******/ 								dependencyId: moduleOutdatedDependencies[i],
/******/ 								error: err
/******/ 							});
/******/ 						}
/******/ 						if(!options.ignoreErrored) {
/******/ 							if(!error)
/******/ 								error = err;
/******/ 						}
/******/ 					}
/******/ 				}
/******/ 			}
/******/ 		}
/******/ 	
/******/ 		// Load self accepted modules
/******/ 		for(i = 0; i < outdatedSelfAcceptedModules.length; i++) {
/******/ 			var item = outdatedSelfAcceptedModules[i];
/******/ 			moduleId = item.module;
/******/ 			hotCurrentParents = [moduleId];
/******/ 			try {
/******/ 				__webpack_require__(moduleId);
/******/ 			} catch(err) {
/******/ 				if(typeof item.errorHandler === "function") {
/******/ 					try {
/******/ 						item.errorHandler(err);
/******/ 					} catch(err2) {
/******/ 						if(options.onErrored) {
/******/ 							options.onErrored({
/******/ 								type: "self-accept-error-handler-errored",
/******/ 								moduleId: moduleId,
/******/ 								error: err2,
/******/ 								orginalError: err
/******/ 							});
/******/ 						}
/******/ 						if(!options.ignoreErrored) {
/******/ 							if(!error)
/******/ 								error = err2;
/******/ 						}
/******/ 						if(!error)
/******/ 							error = err;
/******/ 					}
/******/ 				} else {
/******/ 					if(options.onErrored) {
/******/ 						options.onErrored({
/******/ 							type: "self-accept-errored",
/******/ 							moduleId: moduleId,
/******/ 							error: err
/******/ 						});
/******/ 					}
/******/ 					if(!options.ignoreErrored) {
/******/ 						if(!error)
/******/ 							error = err;
/******/ 					}
/******/ 				}
/******/ 			}
/******/ 		}
/******/ 	
/******/ 		// handle errors in accept handlers and self accepted module load
/******/ 		if(error) {
/******/ 			hotSetStatus("fail");
/******/ 			return Promise.reject(error);
/******/ 		}
/******/ 	
/******/ 		hotSetStatus("idle");
/******/ 		return Promise.resolve(outdatedModules);
/******/ 	}
/******/
/******/ 	// The module cache
/******/ 	var installedModules = {};
/******/
/******/ 	// The require function
/******/ 	function __webpack_require__(moduleId) {
/******/
/******/ 		// Check if module is in cache
/******/ 		if(installedModules[moduleId]) {
/******/ 			return installedModules[moduleId].exports;
/******/ 		}
/******/ 		// Create a new module (and put it into the cache)
/******/ 		var module = installedModules[moduleId] = {
/******/ 			i: moduleId,
/******/ 			l: false,
/******/ 			exports: {},
/******/ 			hot: hotCreateModule(moduleId),
/******/ 			parents: (hotCurrentParentsTemp = hotCurrentParents, hotCurrentParents = [], hotCurrentParentsTemp),
/******/ 			children: []
/******/ 		};
/******/
/******/ 		// Execute the module function
/******/ 		modules[moduleId].call(module.exports, module, module.exports, hotCreateRequire(moduleId));
/******/
/******/ 		// Flag the module as loaded
/******/ 		module.l = true;
/******/
/******/ 		// Return the exports of the module
/******/ 		return module.exports;
/******/ 	}
/******/
/******/
/******/ 	// expose the modules object (__webpack_modules__)
/******/ 	__webpack_require__.m = modules;
/******/
/******/ 	// expose the module cache
/******/ 	__webpack_require__.c = installedModules;
/******/
/******/ 	// identity function for calling harmony imports with the correct context
/******/ 	__webpack_require__.i = function(value) { return value; };
/******/
/******/ 	// define getter function for harmony exports
/******/ 	__webpack_require__.d = function(exports, name, getter) {
/******/ 		if(!__webpack_require__.o(exports, name)) {
/******/ 			Object.defineProperty(exports, name, {
/******/ 				configurable: false,
/******/ 				enumerable: true,
/******/ 				get: getter
/******/ 			});
/******/ 		}
/******/ 	};
/******/
/******/ 	// getDefaultExport function for compatibility with non-harmony modules
/******/ 	__webpack_require__.n = function(module) {
/******/ 		var getter = module && module.__esModule ?
/******/ 			function getDefault() { return module['default']; } :
/******/ 			function getModuleExports() { return module; };
/******/ 		__webpack_require__.d(getter, 'a', getter);
/******/ 		return getter;
/******/ 	};
/******/
/******/ 	// Object.prototype.hasOwnProperty.call
/******/ 	__webpack_require__.o = function(object, property) { return Object.prototype.hasOwnProperty.call(object, property); };
/******/
/******/ 	// __webpack_public_path__
/******/ 	__webpack_require__.p = "dist/";
/******/
/******/ 	// __webpack_hash__
/******/ 	__webpack_require__.h = function() { return hotCurrentHash; };
/******/
/******/ 	// Load entry module and return exports
/******/ 	return hotCreateRequire(63)(__webpack_require__.s = 63);
/******/ })
/************************************************************************/
/******/ ([
/* 0 */
/***/ (function(module, exports) {

module.exports = vendor_4adf5b975b06d7f766a2;

/***/ }),
/* 1 */
/***/ (function(module, exports, __webpack_require__) {

module.exports = (__webpack_require__(0))(3);

/***/ }),
/* 2 */
/***/ (function(module, __webpack_exports__, __webpack_require__) {

"use strict";
/* harmony export (binding) */ __webpack_require__.d(__webpack_exports__, "a", function() { return Strategy; });
var Strategy = (function () {
    function Strategy(p_name, p_app) {
        this.name = p_name;
        this.app = p_app;
        console.log("Strategy ctor()");
    }
    // override these in derived classes BEGIN
    Strategy.prototype.IsMenuItemIdHandled = function (p_subStrategyId) {
        console.log("Strategy.IsMenuItemId(): " + this.name);
        return false;
    };
    Strategy.prototype.OnStrategySelected = function (p_subStrategyId) {
        console.log("Strategy.OnStrategySelected(): " + this.name);
        return false;
    };
    Strategy.prototype.GetHtmlUiName = function (p_subStrategyId) {
        console.log("Strategy.GetUiName(): " + this.name);
        return "Unknown strategy";
    };
    Strategy.prototype.GetTradingViewChartName = function (p_subStrategyId) {
        console.log("Strategy.GetUiName(): " + this.name);
        return "Unknown strategy";
    };
    Strategy.prototype.GetWebApiName = function (p_subStrategyId) {
        console.log("Strategy.GetWebApiName(): " + this.name);
        return "https://www.google.com/";
    };
    Strategy.prototype.GetHelpUri = function (p_subStrategyId) {
        console.log("Strategy.GetHelpUri(): " + this.name);
        return "https://www.google.com/";
    };
    Strategy.prototype.GetStrategyParams = function (p_subStrategyId) {
        console.log("Strategy.GetStrategyParams(): " + this.name);
        return '';
    };
    // override these in derived classes END
    Strategy.prototype.StartBacktest = function (p_http, p_generalInputParameters, p_subStrategyId) {
        var _this = this;
        console.log("StartBacktest() 1");
        if (!this.IsMenuItemIdHandled(p_subStrategyId))
            return;
        console.log("StartBacktest() 2");
        //var url = "http://localhost/qt?StartDate=&EndDate=&strategy=AdaptiveUberVxx&BullishTradingInstrument=Long%20SPY&param=UseKellyLeverage=false;MaxLeverage=1.0&Name=Fomc&Priority=3&Combination=Avg&StartDate=&EndDate=&TradingStartAt=2y&Param=&Name=Holiday&Priority=&Combination=&StartDate=&EndDate=&TradingStartAt=&Param=&Name=TotM&Priority=2&Combination=Avg&StartDate=&EndDate=&TradingStartAt=2y&Param=TrainingTicker=SPY&Name=Connor&Priority=1&Combination=Avg&StartDate=&EndDate=&TradingStartAt=100td&Param=LookbackWindowDays=100;ProbDailyFTThreshold=47
        var url = "/qt?" + p_generalInputParameters + "&strategy=" + this.GetWebApiName(p_subStrategyId) + this.GetStrategyParams(p_subStrategyId);
        console.log("StartBacktest() 2, url: " + url);
        p_http.get(url)
            .map(function (res) { return res.json(); }) // Call map on the response observable to get the parsed people object
            .subscribe(function (data) {
            console.log("StartBacktest(): data received 1: " + data);
            _this.app.tradingViewChartName = _this.GetTradingViewChartName(p_subStrategyId);
            _this.app.ProcessStrategyResult(data);
        }, function (error) {
            console.log("ERROR. StartBacktest(): data received error: " + error);
            _this.app.errorMessage = error;
        });
    };
    return Strategy;
}());



/***/ }),
/* 3 */
/***/ (function(module, exports, __webpack_require__) {

"use strict";

var Observable_1 = __webpack_require__(52);
var map_1 = __webpack_require__(54);
Observable_1.Observable.prototype.map = map_1.map;
//# sourceMappingURL=map.js.map

/***/ }),
/* 4 */
/***/ (function(module, exports) {

/*
	MIT License http://www.opensource.org/licenses/mit-license.php
	Author Tobias Koppers @sokra
*/
// css base code, injected by the css-loader
module.exports = function(useSourceMap) {
	var list = [];

	// return the list of modules as css string
	list.toString = function toString() {
		return this.map(function (item) {
			var content = cssWithMappingToString(item, useSourceMap);
			if(item[2]) {
				return "@media " + item[2] + "{" + content + "}";
			} else {
				return content;
			}
		}).join("");
	};

	// import a list of modules into the list
	list.i = function(modules, mediaQuery) {
		if(typeof modules === "string")
			modules = [[null, modules, ""]];
		var alreadyImportedModules = {};
		for(var i = 0; i < this.length; i++) {
			var id = this[i][0];
			if(typeof id === "number")
				alreadyImportedModules[id] = true;
		}
		for(i = 0; i < modules.length; i++) {
			var item = modules[i];
			// skip already imported module
			// this implementation is not 100% perfect for weird media query combinations
			//  when a module is imported multiple times with different media queries.
			//  I hope this will never occur (Hey this way we have smaller bundles)
			if(typeof item[0] !== "number" || !alreadyImportedModules[item[0]]) {
				if(mediaQuery && !item[2]) {
					item[2] = mediaQuery;
				} else if(mediaQuery) {
					item[2] = "(" + item[2] + ") and (" + mediaQuery + ")";
				}
				list.push(item);
			}
		}
	};
	return list;
};

function cssWithMappingToString(item, useSourceMap) {
	var content = item[1] || '';
	var cssMapping = item[3];
	if (!cssMapping) {
		return content;
	}

	if (useSourceMap && typeof btoa === 'function') {
		var sourceMapping = toComment(cssMapping);
		var sourceURLs = cssMapping.sources.map(function (source) {
			return '/*# sourceURL=' + cssMapping.sourceRoot + source + ' */'
		});

		return [content].concat(sourceURLs).concat([sourceMapping]).join('\n');
	}

	return [content].join('\n');
}

// Adapted from convert-source-map (MIT)
function toComment(sourceMap) {
	// eslint-disable-next-line no-undef
	var base64 = btoa(unescape(encodeURIComponent(JSON.stringify(sourceMap))));
	var data = 'sourceMappingURL=data:application/json;charset=utf-8;base64,' + base64;

	return '/*# ' + data + ' */';
}


/***/ }),
/* 5 */
/***/ (function(module, exports, __webpack_require__) {

module.exports = (__webpack_require__(0))(39);

/***/ }),
/* 6 */
/***/ (function(module, __webpack_exports__, __webpack_require__) {

"use strict";
/* harmony export (binding) */ __webpack_require__.d(__webpack_exports__, "a", function() { return AppComponent; });
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_0__angular_core__ = __webpack_require__(1);
var __decorate = (this && this.__decorate) || function (decorators, target, key, desc) {
    var c = arguments.length, r = c < 3 ? target : desc === null ? desc = Object.getOwnPropertyDescriptor(target, key) : desc, d;
    if (typeof Reflect === "object" && typeof Reflect.decorate === "function") r = Reflect.decorate(decorators, target, key, desc);
    else for (var i = decorators.length - 1; i >= 0; i--) if (d = decorators[i]) r = (c < 3 ? d(r) : c > 3 ? d(target, key, r) : d(target, key)) || r;
    return c > 3 && r && Object.defineProperty(target, key, r), r;
};
var __metadata = (this && this.__metadata) || function (k, v) {
    if (typeof Reflect === "object" && typeof Reflect.metadata === "function") return Reflect.metadata(k, v);
};

var AppComponent = (function () {
    function AppComponent() {
        this.g_sqWebAppName = '';
        if (typeof window == "undefined")
            this.g_sqWebAppName = 'window is not defined.';
        else if (window == null)
            this.g_sqWebAppName = 'window is null.';
        else if ('sqWebAppName' in window)
            this.g_sqWebAppName = window.sqWebAppName;
        else
            this.g_sqWebAppName = 'window is defined, but no window.sqWebAppName.';
    }
    AppComponent = __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Component"])({
            selector: 'app',
            template: __webpack_require__(33),
            styles: [__webpack_require__(45)]
        }),
        __metadata("design:paramtypes", [])
    ], AppComponent);
    return AppComponent;
}());



/***/ }),
/* 7 */
/***/ (function(module, exports) {

var ENTITIES = [['Aacute', [193]], ['aacute', [225]], ['Abreve', [258]], ['abreve', [259]], ['ac', [8766]], ['acd', [8767]], ['acE', [8766, 819]], ['Acirc', [194]], ['acirc', [226]], ['acute', [180]], ['Acy', [1040]], ['acy', [1072]], ['AElig', [198]], ['aelig', [230]], ['af', [8289]], ['Afr', [120068]], ['afr', [120094]], ['Agrave', [192]], ['agrave', [224]], ['alefsym', [8501]], ['aleph', [8501]], ['Alpha', [913]], ['alpha', [945]], ['Amacr', [256]], ['amacr', [257]], ['amalg', [10815]], ['amp', [38]], ['AMP', [38]], ['andand', [10837]], ['And', [10835]], ['and', [8743]], ['andd', [10844]], ['andslope', [10840]], ['andv', [10842]], ['ang', [8736]], ['ange', [10660]], ['angle', [8736]], ['angmsdaa', [10664]], ['angmsdab', [10665]], ['angmsdac', [10666]], ['angmsdad', [10667]], ['angmsdae', [10668]], ['angmsdaf', [10669]], ['angmsdag', [10670]], ['angmsdah', [10671]], ['angmsd', [8737]], ['angrt', [8735]], ['angrtvb', [8894]], ['angrtvbd', [10653]], ['angsph', [8738]], ['angst', [197]], ['angzarr', [9084]], ['Aogon', [260]], ['aogon', [261]], ['Aopf', [120120]], ['aopf', [120146]], ['apacir', [10863]], ['ap', [8776]], ['apE', [10864]], ['ape', [8778]], ['apid', [8779]], ['apos', [39]], ['ApplyFunction', [8289]], ['approx', [8776]], ['approxeq', [8778]], ['Aring', [197]], ['aring', [229]], ['Ascr', [119964]], ['ascr', [119990]], ['Assign', [8788]], ['ast', [42]], ['asymp', [8776]], ['asympeq', [8781]], ['Atilde', [195]], ['atilde', [227]], ['Auml', [196]], ['auml', [228]], ['awconint', [8755]], ['awint', [10769]], ['backcong', [8780]], ['backepsilon', [1014]], ['backprime', [8245]], ['backsim', [8765]], ['backsimeq', [8909]], ['Backslash', [8726]], ['Barv', [10983]], ['barvee', [8893]], ['barwed', [8965]], ['Barwed', [8966]], ['barwedge', [8965]], ['bbrk', [9141]], ['bbrktbrk', [9142]], ['bcong', [8780]], ['Bcy', [1041]], ['bcy', [1073]], ['bdquo', [8222]], ['becaus', [8757]], ['because', [8757]], ['Because', [8757]], ['bemptyv', [10672]], ['bepsi', [1014]], ['bernou', [8492]], ['Bernoullis', [8492]], ['Beta', [914]], ['beta', [946]], ['beth', [8502]], ['between', [8812]], ['Bfr', [120069]], ['bfr', [120095]], ['bigcap', [8898]], ['bigcirc', [9711]], ['bigcup', [8899]], ['bigodot', [10752]], ['bigoplus', [10753]], ['bigotimes', [10754]], ['bigsqcup', [10758]], ['bigstar', [9733]], ['bigtriangledown', [9661]], ['bigtriangleup', [9651]], ['biguplus', [10756]], ['bigvee', [8897]], ['bigwedge', [8896]], ['bkarow', [10509]], ['blacklozenge', [10731]], ['blacksquare', [9642]], ['blacktriangle', [9652]], ['blacktriangledown', [9662]], ['blacktriangleleft', [9666]], ['blacktriangleright', [9656]], ['blank', [9251]], ['blk12', [9618]], ['blk14', [9617]], ['blk34', [9619]], ['block', [9608]], ['bne', [61, 8421]], ['bnequiv', [8801, 8421]], ['bNot', [10989]], ['bnot', [8976]], ['Bopf', [120121]], ['bopf', [120147]], ['bot', [8869]], ['bottom', [8869]], ['bowtie', [8904]], ['boxbox', [10697]], ['boxdl', [9488]], ['boxdL', [9557]], ['boxDl', [9558]], ['boxDL', [9559]], ['boxdr', [9484]], ['boxdR', [9554]], ['boxDr', [9555]], ['boxDR', [9556]], ['boxh', [9472]], ['boxH', [9552]], ['boxhd', [9516]], ['boxHd', [9572]], ['boxhD', [9573]], ['boxHD', [9574]], ['boxhu', [9524]], ['boxHu', [9575]], ['boxhU', [9576]], ['boxHU', [9577]], ['boxminus', [8863]], ['boxplus', [8862]], ['boxtimes', [8864]], ['boxul', [9496]], ['boxuL', [9563]], ['boxUl', [9564]], ['boxUL', [9565]], ['boxur', [9492]], ['boxuR', [9560]], ['boxUr', [9561]], ['boxUR', [9562]], ['boxv', [9474]], ['boxV', [9553]], ['boxvh', [9532]], ['boxvH', [9578]], ['boxVh', [9579]], ['boxVH', [9580]], ['boxvl', [9508]], ['boxvL', [9569]], ['boxVl', [9570]], ['boxVL', [9571]], ['boxvr', [9500]], ['boxvR', [9566]], ['boxVr', [9567]], ['boxVR', [9568]], ['bprime', [8245]], ['breve', [728]], ['Breve', [728]], ['brvbar', [166]], ['bscr', [119991]], ['Bscr', [8492]], ['bsemi', [8271]], ['bsim', [8765]], ['bsime', [8909]], ['bsolb', [10693]], ['bsol', [92]], ['bsolhsub', [10184]], ['bull', [8226]], ['bullet', [8226]], ['bump', [8782]], ['bumpE', [10926]], ['bumpe', [8783]], ['Bumpeq', [8782]], ['bumpeq', [8783]], ['Cacute', [262]], ['cacute', [263]], ['capand', [10820]], ['capbrcup', [10825]], ['capcap', [10827]], ['cap', [8745]], ['Cap', [8914]], ['capcup', [10823]], ['capdot', [10816]], ['CapitalDifferentialD', [8517]], ['caps', [8745, 65024]], ['caret', [8257]], ['caron', [711]], ['Cayleys', [8493]], ['ccaps', [10829]], ['Ccaron', [268]], ['ccaron', [269]], ['Ccedil', [199]], ['ccedil', [231]], ['Ccirc', [264]], ['ccirc', [265]], ['Cconint', [8752]], ['ccups', [10828]], ['ccupssm', [10832]], ['Cdot', [266]], ['cdot', [267]], ['cedil', [184]], ['Cedilla', [184]], ['cemptyv', [10674]], ['cent', [162]], ['centerdot', [183]], ['CenterDot', [183]], ['cfr', [120096]], ['Cfr', [8493]], ['CHcy', [1063]], ['chcy', [1095]], ['check', [10003]], ['checkmark', [10003]], ['Chi', [935]], ['chi', [967]], ['circ', [710]], ['circeq', [8791]], ['circlearrowleft', [8634]], ['circlearrowright', [8635]], ['circledast', [8859]], ['circledcirc', [8858]], ['circleddash', [8861]], ['CircleDot', [8857]], ['circledR', [174]], ['circledS', [9416]], ['CircleMinus', [8854]], ['CirclePlus', [8853]], ['CircleTimes', [8855]], ['cir', [9675]], ['cirE', [10691]], ['cire', [8791]], ['cirfnint', [10768]], ['cirmid', [10991]], ['cirscir', [10690]], ['ClockwiseContourIntegral', [8754]], ['clubs', [9827]], ['clubsuit', [9827]], ['colon', [58]], ['Colon', [8759]], ['Colone', [10868]], ['colone', [8788]], ['coloneq', [8788]], ['comma', [44]], ['commat', [64]], ['comp', [8705]], ['compfn', [8728]], ['complement', [8705]], ['complexes', [8450]], ['cong', [8773]], ['congdot', [10861]], ['Congruent', [8801]], ['conint', [8750]], ['Conint', [8751]], ['ContourIntegral', [8750]], ['copf', [120148]], ['Copf', [8450]], ['coprod', [8720]], ['Coproduct', [8720]], ['copy', [169]], ['COPY', [169]], ['copysr', [8471]], ['CounterClockwiseContourIntegral', [8755]], ['crarr', [8629]], ['cross', [10007]], ['Cross', [10799]], ['Cscr', [119966]], ['cscr', [119992]], ['csub', [10959]], ['csube', [10961]], ['csup', [10960]], ['csupe', [10962]], ['ctdot', [8943]], ['cudarrl', [10552]], ['cudarrr', [10549]], ['cuepr', [8926]], ['cuesc', [8927]], ['cularr', [8630]], ['cularrp', [10557]], ['cupbrcap', [10824]], ['cupcap', [10822]], ['CupCap', [8781]], ['cup', [8746]], ['Cup', [8915]], ['cupcup', [10826]], ['cupdot', [8845]], ['cupor', [10821]], ['cups', [8746, 65024]], ['curarr', [8631]], ['curarrm', [10556]], ['curlyeqprec', [8926]], ['curlyeqsucc', [8927]], ['curlyvee', [8910]], ['curlywedge', [8911]], ['curren', [164]], ['curvearrowleft', [8630]], ['curvearrowright', [8631]], ['cuvee', [8910]], ['cuwed', [8911]], ['cwconint', [8754]], ['cwint', [8753]], ['cylcty', [9005]], ['dagger', [8224]], ['Dagger', [8225]], ['daleth', [8504]], ['darr', [8595]], ['Darr', [8609]], ['dArr', [8659]], ['dash', [8208]], ['Dashv', [10980]], ['dashv', [8867]], ['dbkarow', [10511]], ['dblac', [733]], ['Dcaron', [270]], ['dcaron', [271]], ['Dcy', [1044]], ['dcy', [1076]], ['ddagger', [8225]], ['ddarr', [8650]], ['DD', [8517]], ['dd', [8518]], ['DDotrahd', [10513]], ['ddotseq', [10871]], ['deg', [176]], ['Del', [8711]], ['Delta', [916]], ['delta', [948]], ['demptyv', [10673]], ['dfisht', [10623]], ['Dfr', [120071]], ['dfr', [120097]], ['dHar', [10597]], ['dharl', [8643]], ['dharr', [8642]], ['DiacriticalAcute', [180]], ['DiacriticalDot', [729]], ['DiacriticalDoubleAcute', [733]], ['DiacriticalGrave', [96]], ['DiacriticalTilde', [732]], ['diam', [8900]], ['diamond', [8900]], ['Diamond', [8900]], ['diamondsuit', [9830]], ['diams', [9830]], ['die', [168]], ['DifferentialD', [8518]], ['digamma', [989]], ['disin', [8946]], ['div', [247]], ['divide', [247]], ['divideontimes', [8903]], ['divonx', [8903]], ['DJcy', [1026]], ['djcy', [1106]], ['dlcorn', [8990]], ['dlcrop', [8973]], ['dollar', [36]], ['Dopf', [120123]], ['dopf', [120149]], ['Dot', [168]], ['dot', [729]], ['DotDot', [8412]], ['doteq', [8784]], ['doteqdot', [8785]], ['DotEqual', [8784]], ['dotminus', [8760]], ['dotplus', [8724]], ['dotsquare', [8865]], ['doublebarwedge', [8966]], ['DoubleContourIntegral', [8751]], ['DoubleDot', [168]], ['DoubleDownArrow', [8659]], ['DoubleLeftArrow', [8656]], ['DoubleLeftRightArrow', [8660]], ['DoubleLeftTee', [10980]], ['DoubleLongLeftArrow', [10232]], ['DoubleLongLeftRightArrow', [10234]], ['DoubleLongRightArrow', [10233]], ['DoubleRightArrow', [8658]], ['DoubleRightTee', [8872]], ['DoubleUpArrow', [8657]], ['DoubleUpDownArrow', [8661]], ['DoubleVerticalBar', [8741]], ['DownArrowBar', [10515]], ['downarrow', [8595]], ['DownArrow', [8595]], ['Downarrow', [8659]], ['DownArrowUpArrow', [8693]], ['DownBreve', [785]], ['downdownarrows', [8650]], ['downharpoonleft', [8643]], ['downharpoonright', [8642]], ['DownLeftRightVector', [10576]], ['DownLeftTeeVector', [10590]], ['DownLeftVectorBar', [10582]], ['DownLeftVector', [8637]], ['DownRightTeeVector', [10591]], ['DownRightVectorBar', [10583]], ['DownRightVector', [8641]], ['DownTeeArrow', [8615]], ['DownTee', [8868]], ['drbkarow', [10512]], ['drcorn', [8991]], ['drcrop', [8972]], ['Dscr', [119967]], ['dscr', [119993]], ['DScy', [1029]], ['dscy', [1109]], ['dsol', [10742]], ['Dstrok', [272]], ['dstrok', [273]], ['dtdot', [8945]], ['dtri', [9663]], ['dtrif', [9662]], ['duarr', [8693]], ['duhar', [10607]], ['dwangle', [10662]], ['DZcy', [1039]], ['dzcy', [1119]], ['dzigrarr', [10239]], ['Eacute', [201]], ['eacute', [233]], ['easter', [10862]], ['Ecaron', [282]], ['ecaron', [283]], ['Ecirc', [202]], ['ecirc', [234]], ['ecir', [8790]], ['ecolon', [8789]], ['Ecy', [1069]], ['ecy', [1101]], ['eDDot', [10871]], ['Edot', [278]], ['edot', [279]], ['eDot', [8785]], ['ee', [8519]], ['efDot', [8786]], ['Efr', [120072]], ['efr', [120098]], ['eg', [10906]], ['Egrave', [200]], ['egrave', [232]], ['egs', [10902]], ['egsdot', [10904]], ['el', [10905]], ['Element', [8712]], ['elinters', [9191]], ['ell', [8467]], ['els', [10901]], ['elsdot', [10903]], ['Emacr', [274]], ['emacr', [275]], ['empty', [8709]], ['emptyset', [8709]], ['EmptySmallSquare', [9723]], ['emptyv', [8709]], ['EmptyVerySmallSquare', [9643]], ['emsp13', [8196]], ['emsp14', [8197]], ['emsp', [8195]], ['ENG', [330]], ['eng', [331]], ['ensp', [8194]], ['Eogon', [280]], ['eogon', [281]], ['Eopf', [120124]], ['eopf', [120150]], ['epar', [8917]], ['eparsl', [10723]], ['eplus', [10865]], ['epsi', [949]], ['Epsilon', [917]], ['epsilon', [949]], ['epsiv', [1013]], ['eqcirc', [8790]], ['eqcolon', [8789]], ['eqsim', [8770]], ['eqslantgtr', [10902]], ['eqslantless', [10901]], ['Equal', [10869]], ['equals', [61]], ['EqualTilde', [8770]], ['equest', [8799]], ['Equilibrium', [8652]], ['equiv', [8801]], ['equivDD', [10872]], ['eqvparsl', [10725]], ['erarr', [10609]], ['erDot', [8787]], ['escr', [8495]], ['Escr', [8496]], ['esdot', [8784]], ['Esim', [10867]], ['esim', [8770]], ['Eta', [919]], ['eta', [951]], ['ETH', [208]], ['eth', [240]], ['Euml', [203]], ['euml', [235]], ['euro', [8364]], ['excl', [33]], ['exist', [8707]], ['Exists', [8707]], ['expectation', [8496]], ['exponentiale', [8519]], ['ExponentialE', [8519]], ['fallingdotseq', [8786]], ['Fcy', [1060]], ['fcy', [1092]], ['female', [9792]], ['ffilig', [64259]], ['fflig', [64256]], ['ffllig', [64260]], ['Ffr', [120073]], ['ffr', [120099]], ['filig', [64257]], ['FilledSmallSquare', [9724]], ['FilledVerySmallSquare', [9642]], ['fjlig', [102, 106]], ['flat', [9837]], ['fllig', [64258]], ['fltns', [9649]], ['fnof', [402]], ['Fopf', [120125]], ['fopf', [120151]], ['forall', [8704]], ['ForAll', [8704]], ['fork', [8916]], ['forkv', [10969]], ['Fouriertrf', [8497]], ['fpartint', [10765]], ['frac12', [189]], ['frac13', [8531]], ['frac14', [188]], ['frac15', [8533]], ['frac16', [8537]], ['frac18', [8539]], ['frac23', [8532]], ['frac25', [8534]], ['frac34', [190]], ['frac35', [8535]], ['frac38', [8540]], ['frac45', [8536]], ['frac56', [8538]], ['frac58', [8541]], ['frac78', [8542]], ['frasl', [8260]], ['frown', [8994]], ['fscr', [119995]], ['Fscr', [8497]], ['gacute', [501]], ['Gamma', [915]], ['gamma', [947]], ['Gammad', [988]], ['gammad', [989]], ['gap', [10886]], ['Gbreve', [286]], ['gbreve', [287]], ['Gcedil', [290]], ['Gcirc', [284]], ['gcirc', [285]], ['Gcy', [1043]], ['gcy', [1075]], ['Gdot', [288]], ['gdot', [289]], ['ge', [8805]], ['gE', [8807]], ['gEl', [10892]], ['gel', [8923]], ['geq', [8805]], ['geqq', [8807]], ['geqslant', [10878]], ['gescc', [10921]], ['ges', [10878]], ['gesdot', [10880]], ['gesdoto', [10882]], ['gesdotol', [10884]], ['gesl', [8923, 65024]], ['gesles', [10900]], ['Gfr', [120074]], ['gfr', [120100]], ['gg', [8811]], ['Gg', [8921]], ['ggg', [8921]], ['gimel', [8503]], ['GJcy', [1027]], ['gjcy', [1107]], ['gla', [10917]], ['gl', [8823]], ['glE', [10898]], ['glj', [10916]], ['gnap', [10890]], ['gnapprox', [10890]], ['gne', [10888]], ['gnE', [8809]], ['gneq', [10888]], ['gneqq', [8809]], ['gnsim', [8935]], ['Gopf', [120126]], ['gopf', [120152]], ['grave', [96]], ['GreaterEqual', [8805]], ['GreaterEqualLess', [8923]], ['GreaterFullEqual', [8807]], ['GreaterGreater', [10914]], ['GreaterLess', [8823]], ['GreaterSlantEqual', [10878]], ['GreaterTilde', [8819]], ['Gscr', [119970]], ['gscr', [8458]], ['gsim', [8819]], ['gsime', [10894]], ['gsiml', [10896]], ['gtcc', [10919]], ['gtcir', [10874]], ['gt', [62]], ['GT', [62]], ['Gt', [8811]], ['gtdot', [8919]], ['gtlPar', [10645]], ['gtquest', [10876]], ['gtrapprox', [10886]], ['gtrarr', [10616]], ['gtrdot', [8919]], ['gtreqless', [8923]], ['gtreqqless', [10892]], ['gtrless', [8823]], ['gtrsim', [8819]], ['gvertneqq', [8809, 65024]], ['gvnE', [8809, 65024]], ['Hacek', [711]], ['hairsp', [8202]], ['half', [189]], ['hamilt', [8459]], ['HARDcy', [1066]], ['hardcy', [1098]], ['harrcir', [10568]], ['harr', [8596]], ['hArr', [8660]], ['harrw', [8621]], ['Hat', [94]], ['hbar', [8463]], ['Hcirc', [292]], ['hcirc', [293]], ['hearts', [9829]], ['heartsuit', [9829]], ['hellip', [8230]], ['hercon', [8889]], ['hfr', [120101]], ['Hfr', [8460]], ['HilbertSpace', [8459]], ['hksearow', [10533]], ['hkswarow', [10534]], ['hoarr', [8703]], ['homtht', [8763]], ['hookleftarrow', [8617]], ['hookrightarrow', [8618]], ['hopf', [120153]], ['Hopf', [8461]], ['horbar', [8213]], ['HorizontalLine', [9472]], ['hscr', [119997]], ['Hscr', [8459]], ['hslash', [8463]], ['Hstrok', [294]], ['hstrok', [295]], ['HumpDownHump', [8782]], ['HumpEqual', [8783]], ['hybull', [8259]], ['hyphen', [8208]], ['Iacute', [205]], ['iacute', [237]], ['ic', [8291]], ['Icirc', [206]], ['icirc', [238]], ['Icy', [1048]], ['icy', [1080]], ['Idot', [304]], ['IEcy', [1045]], ['iecy', [1077]], ['iexcl', [161]], ['iff', [8660]], ['ifr', [120102]], ['Ifr', [8465]], ['Igrave', [204]], ['igrave', [236]], ['ii', [8520]], ['iiiint', [10764]], ['iiint', [8749]], ['iinfin', [10716]], ['iiota', [8489]], ['IJlig', [306]], ['ijlig', [307]], ['Imacr', [298]], ['imacr', [299]], ['image', [8465]], ['ImaginaryI', [8520]], ['imagline', [8464]], ['imagpart', [8465]], ['imath', [305]], ['Im', [8465]], ['imof', [8887]], ['imped', [437]], ['Implies', [8658]], ['incare', [8453]], ['in', [8712]], ['infin', [8734]], ['infintie', [10717]], ['inodot', [305]], ['intcal', [8890]], ['int', [8747]], ['Int', [8748]], ['integers', [8484]], ['Integral', [8747]], ['intercal', [8890]], ['Intersection', [8898]], ['intlarhk', [10775]], ['intprod', [10812]], ['InvisibleComma', [8291]], ['InvisibleTimes', [8290]], ['IOcy', [1025]], ['iocy', [1105]], ['Iogon', [302]], ['iogon', [303]], ['Iopf', [120128]], ['iopf', [120154]], ['Iota', [921]], ['iota', [953]], ['iprod', [10812]], ['iquest', [191]], ['iscr', [119998]], ['Iscr', [8464]], ['isin', [8712]], ['isindot', [8949]], ['isinE', [8953]], ['isins', [8948]], ['isinsv', [8947]], ['isinv', [8712]], ['it', [8290]], ['Itilde', [296]], ['itilde', [297]], ['Iukcy', [1030]], ['iukcy', [1110]], ['Iuml', [207]], ['iuml', [239]], ['Jcirc', [308]], ['jcirc', [309]], ['Jcy', [1049]], ['jcy', [1081]], ['Jfr', [120077]], ['jfr', [120103]], ['jmath', [567]], ['Jopf', [120129]], ['jopf', [120155]], ['Jscr', [119973]], ['jscr', [119999]], ['Jsercy', [1032]], ['jsercy', [1112]], ['Jukcy', [1028]], ['jukcy', [1108]], ['Kappa', [922]], ['kappa', [954]], ['kappav', [1008]], ['Kcedil', [310]], ['kcedil', [311]], ['Kcy', [1050]], ['kcy', [1082]], ['Kfr', [120078]], ['kfr', [120104]], ['kgreen', [312]], ['KHcy', [1061]], ['khcy', [1093]], ['KJcy', [1036]], ['kjcy', [1116]], ['Kopf', [120130]], ['kopf', [120156]], ['Kscr', [119974]], ['kscr', [120000]], ['lAarr', [8666]], ['Lacute', [313]], ['lacute', [314]], ['laemptyv', [10676]], ['lagran', [8466]], ['Lambda', [923]], ['lambda', [955]], ['lang', [10216]], ['Lang', [10218]], ['langd', [10641]], ['langle', [10216]], ['lap', [10885]], ['Laplacetrf', [8466]], ['laquo', [171]], ['larrb', [8676]], ['larrbfs', [10527]], ['larr', [8592]], ['Larr', [8606]], ['lArr', [8656]], ['larrfs', [10525]], ['larrhk', [8617]], ['larrlp', [8619]], ['larrpl', [10553]], ['larrsim', [10611]], ['larrtl', [8610]], ['latail', [10521]], ['lAtail', [10523]], ['lat', [10923]], ['late', [10925]], ['lates', [10925, 65024]], ['lbarr', [10508]], ['lBarr', [10510]], ['lbbrk', [10098]], ['lbrace', [123]], ['lbrack', [91]], ['lbrke', [10635]], ['lbrksld', [10639]], ['lbrkslu', [10637]], ['Lcaron', [317]], ['lcaron', [318]], ['Lcedil', [315]], ['lcedil', [316]], ['lceil', [8968]], ['lcub', [123]], ['Lcy', [1051]], ['lcy', [1083]], ['ldca', [10550]], ['ldquo', [8220]], ['ldquor', [8222]], ['ldrdhar', [10599]], ['ldrushar', [10571]], ['ldsh', [8626]], ['le', [8804]], ['lE', [8806]], ['LeftAngleBracket', [10216]], ['LeftArrowBar', [8676]], ['leftarrow', [8592]], ['LeftArrow', [8592]], ['Leftarrow', [8656]], ['LeftArrowRightArrow', [8646]], ['leftarrowtail', [8610]], ['LeftCeiling', [8968]], ['LeftDoubleBracket', [10214]], ['LeftDownTeeVector', [10593]], ['LeftDownVectorBar', [10585]], ['LeftDownVector', [8643]], ['LeftFloor', [8970]], ['leftharpoondown', [8637]], ['leftharpoonup', [8636]], ['leftleftarrows', [8647]], ['leftrightarrow', [8596]], ['LeftRightArrow', [8596]], ['Leftrightarrow', [8660]], ['leftrightarrows', [8646]], ['leftrightharpoons', [8651]], ['leftrightsquigarrow', [8621]], ['LeftRightVector', [10574]], ['LeftTeeArrow', [8612]], ['LeftTee', [8867]], ['LeftTeeVector', [10586]], ['leftthreetimes', [8907]], ['LeftTriangleBar', [10703]], ['LeftTriangle', [8882]], ['LeftTriangleEqual', [8884]], ['LeftUpDownVector', [10577]], ['LeftUpTeeVector', [10592]], ['LeftUpVectorBar', [10584]], ['LeftUpVector', [8639]], ['LeftVectorBar', [10578]], ['LeftVector', [8636]], ['lEg', [10891]], ['leg', [8922]], ['leq', [8804]], ['leqq', [8806]], ['leqslant', [10877]], ['lescc', [10920]], ['les', [10877]], ['lesdot', [10879]], ['lesdoto', [10881]], ['lesdotor', [10883]], ['lesg', [8922, 65024]], ['lesges', [10899]], ['lessapprox', [10885]], ['lessdot', [8918]], ['lesseqgtr', [8922]], ['lesseqqgtr', [10891]], ['LessEqualGreater', [8922]], ['LessFullEqual', [8806]], ['LessGreater', [8822]], ['lessgtr', [8822]], ['LessLess', [10913]], ['lesssim', [8818]], ['LessSlantEqual', [10877]], ['LessTilde', [8818]], ['lfisht', [10620]], ['lfloor', [8970]], ['Lfr', [120079]], ['lfr', [120105]], ['lg', [8822]], ['lgE', [10897]], ['lHar', [10594]], ['lhard', [8637]], ['lharu', [8636]], ['lharul', [10602]], ['lhblk', [9604]], ['LJcy', [1033]], ['ljcy', [1113]], ['llarr', [8647]], ['ll', [8810]], ['Ll', [8920]], ['llcorner', [8990]], ['Lleftarrow', [8666]], ['llhard', [10603]], ['lltri', [9722]], ['Lmidot', [319]], ['lmidot', [320]], ['lmoustache', [9136]], ['lmoust', [9136]], ['lnap', [10889]], ['lnapprox', [10889]], ['lne', [10887]], ['lnE', [8808]], ['lneq', [10887]], ['lneqq', [8808]], ['lnsim', [8934]], ['loang', [10220]], ['loarr', [8701]], ['lobrk', [10214]], ['longleftarrow', [10229]], ['LongLeftArrow', [10229]], ['Longleftarrow', [10232]], ['longleftrightarrow', [10231]], ['LongLeftRightArrow', [10231]], ['Longleftrightarrow', [10234]], ['longmapsto', [10236]], ['longrightarrow', [10230]], ['LongRightArrow', [10230]], ['Longrightarrow', [10233]], ['looparrowleft', [8619]], ['looparrowright', [8620]], ['lopar', [10629]], ['Lopf', [120131]], ['lopf', [120157]], ['loplus', [10797]], ['lotimes', [10804]], ['lowast', [8727]], ['lowbar', [95]], ['LowerLeftArrow', [8601]], ['LowerRightArrow', [8600]], ['loz', [9674]], ['lozenge', [9674]], ['lozf', [10731]], ['lpar', [40]], ['lparlt', [10643]], ['lrarr', [8646]], ['lrcorner', [8991]], ['lrhar', [8651]], ['lrhard', [10605]], ['lrm', [8206]], ['lrtri', [8895]], ['lsaquo', [8249]], ['lscr', [120001]], ['Lscr', [8466]], ['lsh', [8624]], ['Lsh', [8624]], ['lsim', [8818]], ['lsime', [10893]], ['lsimg', [10895]], ['lsqb', [91]], ['lsquo', [8216]], ['lsquor', [8218]], ['Lstrok', [321]], ['lstrok', [322]], ['ltcc', [10918]], ['ltcir', [10873]], ['lt', [60]], ['LT', [60]], ['Lt', [8810]], ['ltdot', [8918]], ['lthree', [8907]], ['ltimes', [8905]], ['ltlarr', [10614]], ['ltquest', [10875]], ['ltri', [9667]], ['ltrie', [8884]], ['ltrif', [9666]], ['ltrPar', [10646]], ['lurdshar', [10570]], ['luruhar', [10598]], ['lvertneqq', [8808, 65024]], ['lvnE', [8808, 65024]], ['macr', [175]], ['male', [9794]], ['malt', [10016]], ['maltese', [10016]], ['Map', [10501]], ['map', [8614]], ['mapsto', [8614]], ['mapstodown', [8615]], ['mapstoleft', [8612]], ['mapstoup', [8613]], ['marker', [9646]], ['mcomma', [10793]], ['Mcy', [1052]], ['mcy', [1084]], ['mdash', [8212]], ['mDDot', [8762]], ['measuredangle', [8737]], ['MediumSpace', [8287]], ['Mellintrf', [8499]], ['Mfr', [120080]], ['mfr', [120106]], ['mho', [8487]], ['micro', [181]], ['midast', [42]], ['midcir', [10992]], ['mid', [8739]], ['middot', [183]], ['minusb', [8863]], ['minus', [8722]], ['minusd', [8760]], ['minusdu', [10794]], ['MinusPlus', [8723]], ['mlcp', [10971]], ['mldr', [8230]], ['mnplus', [8723]], ['models', [8871]], ['Mopf', [120132]], ['mopf', [120158]], ['mp', [8723]], ['mscr', [120002]], ['Mscr', [8499]], ['mstpos', [8766]], ['Mu', [924]], ['mu', [956]], ['multimap', [8888]], ['mumap', [8888]], ['nabla', [8711]], ['Nacute', [323]], ['nacute', [324]], ['nang', [8736, 8402]], ['nap', [8777]], ['napE', [10864, 824]], ['napid', [8779, 824]], ['napos', [329]], ['napprox', [8777]], ['natural', [9838]], ['naturals', [8469]], ['natur', [9838]], ['nbsp', [160]], ['nbump', [8782, 824]], ['nbumpe', [8783, 824]], ['ncap', [10819]], ['Ncaron', [327]], ['ncaron', [328]], ['Ncedil', [325]], ['ncedil', [326]], ['ncong', [8775]], ['ncongdot', [10861, 824]], ['ncup', [10818]], ['Ncy', [1053]], ['ncy', [1085]], ['ndash', [8211]], ['nearhk', [10532]], ['nearr', [8599]], ['neArr', [8663]], ['nearrow', [8599]], ['ne', [8800]], ['nedot', [8784, 824]], ['NegativeMediumSpace', [8203]], ['NegativeThickSpace', [8203]], ['NegativeThinSpace', [8203]], ['NegativeVeryThinSpace', [8203]], ['nequiv', [8802]], ['nesear', [10536]], ['nesim', [8770, 824]], ['NestedGreaterGreater', [8811]], ['NestedLessLess', [8810]], ['nexist', [8708]], ['nexists', [8708]], ['Nfr', [120081]], ['nfr', [120107]], ['ngE', [8807, 824]], ['nge', [8817]], ['ngeq', [8817]], ['ngeqq', [8807, 824]], ['ngeqslant', [10878, 824]], ['nges', [10878, 824]], ['nGg', [8921, 824]], ['ngsim', [8821]], ['nGt', [8811, 8402]], ['ngt', [8815]], ['ngtr', [8815]], ['nGtv', [8811, 824]], ['nharr', [8622]], ['nhArr', [8654]], ['nhpar', [10994]], ['ni', [8715]], ['nis', [8956]], ['nisd', [8954]], ['niv', [8715]], ['NJcy', [1034]], ['njcy', [1114]], ['nlarr', [8602]], ['nlArr', [8653]], ['nldr', [8229]], ['nlE', [8806, 824]], ['nle', [8816]], ['nleftarrow', [8602]], ['nLeftarrow', [8653]], ['nleftrightarrow', [8622]], ['nLeftrightarrow', [8654]], ['nleq', [8816]], ['nleqq', [8806, 824]], ['nleqslant', [10877, 824]], ['nles', [10877, 824]], ['nless', [8814]], ['nLl', [8920, 824]], ['nlsim', [8820]], ['nLt', [8810, 8402]], ['nlt', [8814]], ['nltri', [8938]], ['nltrie', [8940]], ['nLtv', [8810, 824]], ['nmid', [8740]], ['NoBreak', [8288]], ['NonBreakingSpace', [160]], ['nopf', [120159]], ['Nopf', [8469]], ['Not', [10988]], ['not', [172]], ['NotCongruent', [8802]], ['NotCupCap', [8813]], ['NotDoubleVerticalBar', [8742]], ['NotElement', [8713]], ['NotEqual', [8800]], ['NotEqualTilde', [8770, 824]], ['NotExists', [8708]], ['NotGreater', [8815]], ['NotGreaterEqual', [8817]], ['NotGreaterFullEqual', [8807, 824]], ['NotGreaterGreater', [8811, 824]], ['NotGreaterLess', [8825]], ['NotGreaterSlantEqual', [10878, 824]], ['NotGreaterTilde', [8821]], ['NotHumpDownHump', [8782, 824]], ['NotHumpEqual', [8783, 824]], ['notin', [8713]], ['notindot', [8949, 824]], ['notinE', [8953, 824]], ['notinva', [8713]], ['notinvb', [8951]], ['notinvc', [8950]], ['NotLeftTriangleBar', [10703, 824]], ['NotLeftTriangle', [8938]], ['NotLeftTriangleEqual', [8940]], ['NotLess', [8814]], ['NotLessEqual', [8816]], ['NotLessGreater', [8824]], ['NotLessLess', [8810, 824]], ['NotLessSlantEqual', [10877, 824]], ['NotLessTilde', [8820]], ['NotNestedGreaterGreater', [10914, 824]], ['NotNestedLessLess', [10913, 824]], ['notni', [8716]], ['notniva', [8716]], ['notnivb', [8958]], ['notnivc', [8957]], ['NotPrecedes', [8832]], ['NotPrecedesEqual', [10927, 824]], ['NotPrecedesSlantEqual', [8928]], ['NotReverseElement', [8716]], ['NotRightTriangleBar', [10704, 824]], ['NotRightTriangle', [8939]], ['NotRightTriangleEqual', [8941]], ['NotSquareSubset', [8847, 824]], ['NotSquareSubsetEqual', [8930]], ['NotSquareSuperset', [8848, 824]], ['NotSquareSupersetEqual', [8931]], ['NotSubset', [8834, 8402]], ['NotSubsetEqual', [8840]], ['NotSucceeds', [8833]], ['NotSucceedsEqual', [10928, 824]], ['NotSucceedsSlantEqual', [8929]], ['NotSucceedsTilde', [8831, 824]], ['NotSuperset', [8835, 8402]], ['NotSupersetEqual', [8841]], ['NotTilde', [8769]], ['NotTildeEqual', [8772]], ['NotTildeFullEqual', [8775]], ['NotTildeTilde', [8777]], ['NotVerticalBar', [8740]], ['nparallel', [8742]], ['npar', [8742]], ['nparsl', [11005, 8421]], ['npart', [8706, 824]], ['npolint', [10772]], ['npr', [8832]], ['nprcue', [8928]], ['nprec', [8832]], ['npreceq', [10927, 824]], ['npre', [10927, 824]], ['nrarrc', [10547, 824]], ['nrarr', [8603]], ['nrArr', [8655]], ['nrarrw', [8605, 824]], ['nrightarrow', [8603]], ['nRightarrow', [8655]], ['nrtri', [8939]], ['nrtrie', [8941]], ['nsc', [8833]], ['nsccue', [8929]], ['nsce', [10928, 824]], ['Nscr', [119977]], ['nscr', [120003]], ['nshortmid', [8740]], ['nshortparallel', [8742]], ['nsim', [8769]], ['nsime', [8772]], ['nsimeq', [8772]], ['nsmid', [8740]], ['nspar', [8742]], ['nsqsube', [8930]], ['nsqsupe', [8931]], ['nsub', [8836]], ['nsubE', [10949, 824]], ['nsube', [8840]], ['nsubset', [8834, 8402]], ['nsubseteq', [8840]], ['nsubseteqq', [10949, 824]], ['nsucc', [8833]], ['nsucceq', [10928, 824]], ['nsup', [8837]], ['nsupE', [10950, 824]], ['nsupe', [8841]], ['nsupset', [8835, 8402]], ['nsupseteq', [8841]], ['nsupseteqq', [10950, 824]], ['ntgl', [8825]], ['Ntilde', [209]], ['ntilde', [241]], ['ntlg', [8824]], ['ntriangleleft', [8938]], ['ntrianglelefteq', [8940]], ['ntriangleright', [8939]], ['ntrianglerighteq', [8941]], ['Nu', [925]], ['nu', [957]], ['num', [35]], ['numero', [8470]], ['numsp', [8199]], ['nvap', [8781, 8402]], ['nvdash', [8876]], ['nvDash', [8877]], ['nVdash', [8878]], ['nVDash', [8879]], ['nvge', [8805, 8402]], ['nvgt', [62, 8402]], ['nvHarr', [10500]], ['nvinfin', [10718]], ['nvlArr', [10498]], ['nvle', [8804, 8402]], ['nvlt', [60, 8402]], ['nvltrie', [8884, 8402]], ['nvrArr', [10499]], ['nvrtrie', [8885, 8402]], ['nvsim', [8764, 8402]], ['nwarhk', [10531]], ['nwarr', [8598]], ['nwArr', [8662]], ['nwarrow', [8598]], ['nwnear', [10535]], ['Oacute', [211]], ['oacute', [243]], ['oast', [8859]], ['Ocirc', [212]], ['ocirc', [244]], ['ocir', [8858]], ['Ocy', [1054]], ['ocy', [1086]], ['odash', [8861]], ['Odblac', [336]], ['odblac', [337]], ['odiv', [10808]], ['odot', [8857]], ['odsold', [10684]], ['OElig', [338]], ['oelig', [339]], ['ofcir', [10687]], ['Ofr', [120082]], ['ofr', [120108]], ['ogon', [731]], ['Ograve', [210]], ['ograve', [242]], ['ogt', [10689]], ['ohbar', [10677]], ['ohm', [937]], ['oint', [8750]], ['olarr', [8634]], ['olcir', [10686]], ['olcross', [10683]], ['oline', [8254]], ['olt', [10688]], ['Omacr', [332]], ['omacr', [333]], ['Omega', [937]], ['omega', [969]], ['Omicron', [927]], ['omicron', [959]], ['omid', [10678]], ['ominus', [8854]], ['Oopf', [120134]], ['oopf', [120160]], ['opar', [10679]], ['OpenCurlyDoubleQuote', [8220]], ['OpenCurlyQuote', [8216]], ['operp', [10681]], ['oplus', [8853]], ['orarr', [8635]], ['Or', [10836]], ['or', [8744]], ['ord', [10845]], ['order', [8500]], ['orderof', [8500]], ['ordf', [170]], ['ordm', [186]], ['origof', [8886]], ['oror', [10838]], ['orslope', [10839]], ['orv', [10843]], ['oS', [9416]], ['Oscr', [119978]], ['oscr', [8500]], ['Oslash', [216]], ['oslash', [248]], ['osol', [8856]], ['Otilde', [213]], ['otilde', [245]], ['otimesas', [10806]], ['Otimes', [10807]], ['otimes', [8855]], ['Ouml', [214]], ['ouml', [246]], ['ovbar', [9021]], ['OverBar', [8254]], ['OverBrace', [9182]], ['OverBracket', [9140]], ['OverParenthesis', [9180]], ['para', [182]], ['parallel', [8741]], ['par', [8741]], ['parsim', [10995]], ['parsl', [11005]], ['part', [8706]], ['PartialD', [8706]], ['Pcy', [1055]], ['pcy', [1087]], ['percnt', [37]], ['period', [46]], ['permil', [8240]], ['perp', [8869]], ['pertenk', [8241]], ['Pfr', [120083]], ['pfr', [120109]], ['Phi', [934]], ['phi', [966]], ['phiv', [981]], ['phmmat', [8499]], ['phone', [9742]], ['Pi', [928]], ['pi', [960]], ['pitchfork', [8916]], ['piv', [982]], ['planck', [8463]], ['planckh', [8462]], ['plankv', [8463]], ['plusacir', [10787]], ['plusb', [8862]], ['pluscir', [10786]], ['plus', [43]], ['plusdo', [8724]], ['plusdu', [10789]], ['pluse', [10866]], ['PlusMinus', [177]], ['plusmn', [177]], ['plussim', [10790]], ['plustwo', [10791]], ['pm', [177]], ['Poincareplane', [8460]], ['pointint', [10773]], ['popf', [120161]], ['Popf', [8473]], ['pound', [163]], ['prap', [10935]], ['Pr', [10939]], ['pr', [8826]], ['prcue', [8828]], ['precapprox', [10935]], ['prec', [8826]], ['preccurlyeq', [8828]], ['Precedes', [8826]], ['PrecedesEqual', [10927]], ['PrecedesSlantEqual', [8828]], ['PrecedesTilde', [8830]], ['preceq', [10927]], ['precnapprox', [10937]], ['precneqq', [10933]], ['precnsim', [8936]], ['pre', [10927]], ['prE', [10931]], ['precsim', [8830]], ['prime', [8242]], ['Prime', [8243]], ['primes', [8473]], ['prnap', [10937]], ['prnE', [10933]], ['prnsim', [8936]], ['prod', [8719]], ['Product', [8719]], ['profalar', [9006]], ['profline', [8978]], ['profsurf', [8979]], ['prop', [8733]], ['Proportional', [8733]], ['Proportion', [8759]], ['propto', [8733]], ['prsim', [8830]], ['prurel', [8880]], ['Pscr', [119979]], ['pscr', [120005]], ['Psi', [936]], ['psi', [968]], ['puncsp', [8200]], ['Qfr', [120084]], ['qfr', [120110]], ['qint', [10764]], ['qopf', [120162]], ['Qopf', [8474]], ['qprime', [8279]], ['Qscr', [119980]], ['qscr', [120006]], ['quaternions', [8461]], ['quatint', [10774]], ['quest', [63]], ['questeq', [8799]], ['quot', [34]], ['QUOT', [34]], ['rAarr', [8667]], ['race', [8765, 817]], ['Racute', [340]], ['racute', [341]], ['radic', [8730]], ['raemptyv', [10675]], ['rang', [10217]], ['Rang', [10219]], ['rangd', [10642]], ['range', [10661]], ['rangle', [10217]], ['raquo', [187]], ['rarrap', [10613]], ['rarrb', [8677]], ['rarrbfs', [10528]], ['rarrc', [10547]], ['rarr', [8594]], ['Rarr', [8608]], ['rArr', [8658]], ['rarrfs', [10526]], ['rarrhk', [8618]], ['rarrlp', [8620]], ['rarrpl', [10565]], ['rarrsim', [10612]], ['Rarrtl', [10518]], ['rarrtl', [8611]], ['rarrw', [8605]], ['ratail', [10522]], ['rAtail', [10524]], ['ratio', [8758]], ['rationals', [8474]], ['rbarr', [10509]], ['rBarr', [10511]], ['RBarr', [10512]], ['rbbrk', [10099]], ['rbrace', [125]], ['rbrack', [93]], ['rbrke', [10636]], ['rbrksld', [10638]], ['rbrkslu', [10640]], ['Rcaron', [344]], ['rcaron', [345]], ['Rcedil', [342]], ['rcedil', [343]], ['rceil', [8969]], ['rcub', [125]], ['Rcy', [1056]], ['rcy', [1088]], ['rdca', [10551]], ['rdldhar', [10601]], ['rdquo', [8221]], ['rdquor', [8221]], ['CloseCurlyDoubleQuote', [8221]], ['rdsh', [8627]], ['real', [8476]], ['realine', [8475]], ['realpart', [8476]], ['reals', [8477]], ['Re', [8476]], ['rect', [9645]], ['reg', [174]], ['REG', [174]], ['ReverseElement', [8715]], ['ReverseEquilibrium', [8651]], ['ReverseUpEquilibrium', [10607]], ['rfisht', [10621]], ['rfloor', [8971]], ['rfr', [120111]], ['Rfr', [8476]], ['rHar', [10596]], ['rhard', [8641]], ['rharu', [8640]], ['rharul', [10604]], ['Rho', [929]], ['rho', [961]], ['rhov', [1009]], ['RightAngleBracket', [10217]], ['RightArrowBar', [8677]], ['rightarrow', [8594]], ['RightArrow', [8594]], ['Rightarrow', [8658]], ['RightArrowLeftArrow', [8644]], ['rightarrowtail', [8611]], ['RightCeiling', [8969]], ['RightDoubleBracket', [10215]], ['RightDownTeeVector', [10589]], ['RightDownVectorBar', [10581]], ['RightDownVector', [8642]], ['RightFloor', [8971]], ['rightharpoondown', [8641]], ['rightharpoonup', [8640]], ['rightleftarrows', [8644]], ['rightleftharpoons', [8652]], ['rightrightarrows', [8649]], ['rightsquigarrow', [8605]], ['RightTeeArrow', [8614]], ['RightTee', [8866]], ['RightTeeVector', [10587]], ['rightthreetimes', [8908]], ['RightTriangleBar', [10704]], ['RightTriangle', [8883]], ['RightTriangleEqual', [8885]], ['RightUpDownVector', [10575]], ['RightUpTeeVector', [10588]], ['RightUpVectorBar', [10580]], ['RightUpVector', [8638]], ['RightVectorBar', [10579]], ['RightVector', [8640]], ['ring', [730]], ['risingdotseq', [8787]], ['rlarr', [8644]], ['rlhar', [8652]], ['rlm', [8207]], ['rmoustache', [9137]], ['rmoust', [9137]], ['rnmid', [10990]], ['roang', [10221]], ['roarr', [8702]], ['robrk', [10215]], ['ropar', [10630]], ['ropf', [120163]], ['Ropf', [8477]], ['roplus', [10798]], ['rotimes', [10805]], ['RoundImplies', [10608]], ['rpar', [41]], ['rpargt', [10644]], ['rppolint', [10770]], ['rrarr', [8649]], ['Rrightarrow', [8667]], ['rsaquo', [8250]], ['rscr', [120007]], ['Rscr', [8475]], ['rsh', [8625]], ['Rsh', [8625]], ['rsqb', [93]], ['rsquo', [8217]], ['rsquor', [8217]], ['CloseCurlyQuote', [8217]], ['rthree', [8908]], ['rtimes', [8906]], ['rtri', [9657]], ['rtrie', [8885]], ['rtrif', [9656]], ['rtriltri', [10702]], ['RuleDelayed', [10740]], ['ruluhar', [10600]], ['rx', [8478]], ['Sacute', [346]], ['sacute', [347]], ['sbquo', [8218]], ['scap', [10936]], ['Scaron', [352]], ['scaron', [353]], ['Sc', [10940]], ['sc', [8827]], ['sccue', [8829]], ['sce', [10928]], ['scE', [10932]], ['Scedil', [350]], ['scedil', [351]], ['Scirc', [348]], ['scirc', [349]], ['scnap', [10938]], ['scnE', [10934]], ['scnsim', [8937]], ['scpolint', [10771]], ['scsim', [8831]], ['Scy', [1057]], ['scy', [1089]], ['sdotb', [8865]], ['sdot', [8901]], ['sdote', [10854]], ['searhk', [10533]], ['searr', [8600]], ['seArr', [8664]], ['searrow', [8600]], ['sect', [167]], ['semi', [59]], ['seswar', [10537]], ['setminus', [8726]], ['setmn', [8726]], ['sext', [10038]], ['Sfr', [120086]], ['sfr', [120112]], ['sfrown', [8994]], ['sharp', [9839]], ['SHCHcy', [1065]], ['shchcy', [1097]], ['SHcy', [1064]], ['shcy', [1096]], ['ShortDownArrow', [8595]], ['ShortLeftArrow', [8592]], ['shortmid', [8739]], ['shortparallel', [8741]], ['ShortRightArrow', [8594]], ['ShortUpArrow', [8593]], ['shy', [173]], ['Sigma', [931]], ['sigma', [963]], ['sigmaf', [962]], ['sigmav', [962]], ['sim', [8764]], ['simdot', [10858]], ['sime', [8771]], ['simeq', [8771]], ['simg', [10910]], ['simgE', [10912]], ['siml', [10909]], ['simlE', [10911]], ['simne', [8774]], ['simplus', [10788]], ['simrarr', [10610]], ['slarr', [8592]], ['SmallCircle', [8728]], ['smallsetminus', [8726]], ['smashp', [10803]], ['smeparsl', [10724]], ['smid', [8739]], ['smile', [8995]], ['smt', [10922]], ['smte', [10924]], ['smtes', [10924, 65024]], ['SOFTcy', [1068]], ['softcy', [1100]], ['solbar', [9023]], ['solb', [10692]], ['sol', [47]], ['Sopf', [120138]], ['sopf', [120164]], ['spades', [9824]], ['spadesuit', [9824]], ['spar', [8741]], ['sqcap', [8851]], ['sqcaps', [8851, 65024]], ['sqcup', [8852]], ['sqcups', [8852, 65024]], ['Sqrt', [8730]], ['sqsub', [8847]], ['sqsube', [8849]], ['sqsubset', [8847]], ['sqsubseteq', [8849]], ['sqsup', [8848]], ['sqsupe', [8850]], ['sqsupset', [8848]], ['sqsupseteq', [8850]], ['square', [9633]], ['Square', [9633]], ['SquareIntersection', [8851]], ['SquareSubset', [8847]], ['SquareSubsetEqual', [8849]], ['SquareSuperset', [8848]], ['SquareSupersetEqual', [8850]], ['SquareUnion', [8852]], ['squarf', [9642]], ['squ', [9633]], ['squf', [9642]], ['srarr', [8594]], ['Sscr', [119982]], ['sscr', [120008]], ['ssetmn', [8726]], ['ssmile', [8995]], ['sstarf', [8902]], ['Star', [8902]], ['star', [9734]], ['starf', [9733]], ['straightepsilon', [1013]], ['straightphi', [981]], ['strns', [175]], ['sub', [8834]], ['Sub', [8912]], ['subdot', [10941]], ['subE', [10949]], ['sube', [8838]], ['subedot', [10947]], ['submult', [10945]], ['subnE', [10955]], ['subne', [8842]], ['subplus', [10943]], ['subrarr', [10617]], ['subset', [8834]], ['Subset', [8912]], ['subseteq', [8838]], ['subseteqq', [10949]], ['SubsetEqual', [8838]], ['subsetneq', [8842]], ['subsetneqq', [10955]], ['subsim', [10951]], ['subsub', [10965]], ['subsup', [10963]], ['succapprox', [10936]], ['succ', [8827]], ['succcurlyeq', [8829]], ['Succeeds', [8827]], ['SucceedsEqual', [10928]], ['SucceedsSlantEqual', [8829]], ['SucceedsTilde', [8831]], ['succeq', [10928]], ['succnapprox', [10938]], ['succneqq', [10934]], ['succnsim', [8937]], ['succsim', [8831]], ['SuchThat', [8715]], ['sum', [8721]], ['Sum', [8721]], ['sung', [9834]], ['sup1', [185]], ['sup2', [178]], ['sup3', [179]], ['sup', [8835]], ['Sup', [8913]], ['supdot', [10942]], ['supdsub', [10968]], ['supE', [10950]], ['supe', [8839]], ['supedot', [10948]], ['Superset', [8835]], ['SupersetEqual', [8839]], ['suphsol', [10185]], ['suphsub', [10967]], ['suplarr', [10619]], ['supmult', [10946]], ['supnE', [10956]], ['supne', [8843]], ['supplus', [10944]], ['supset', [8835]], ['Supset', [8913]], ['supseteq', [8839]], ['supseteqq', [10950]], ['supsetneq', [8843]], ['supsetneqq', [10956]], ['supsim', [10952]], ['supsub', [10964]], ['supsup', [10966]], ['swarhk', [10534]], ['swarr', [8601]], ['swArr', [8665]], ['swarrow', [8601]], ['swnwar', [10538]], ['szlig', [223]], ['Tab', [9]], ['target', [8982]], ['Tau', [932]], ['tau', [964]], ['tbrk', [9140]], ['Tcaron', [356]], ['tcaron', [357]], ['Tcedil', [354]], ['tcedil', [355]], ['Tcy', [1058]], ['tcy', [1090]], ['tdot', [8411]], ['telrec', [8981]], ['Tfr', [120087]], ['tfr', [120113]], ['there4', [8756]], ['therefore', [8756]], ['Therefore', [8756]], ['Theta', [920]], ['theta', [952]], ['thetasym', [977]], ['thetav', [977]], ['thickapprox', [8776]], ['thicksim', [8764]], ['ThickSpace', [8287, 8202]], ['ThinSpace', [8201]], ['thinsp', [8201]], ['thkap', [8776]], ['thksim', [8764]], ['THORN', [222]], ['thorn', [254]], ['tilde', [732]], ['Tilde', [8764]], ['TildeEqual', [8771]], ['TildeFullEqual', [8773]], ['TildeTilde', [8776]], ['timesbar', [10801]], ['timesb', [8864]], ['times', [215]], ['timesd', [10800]], ['tint', [8749]], ['toea', [10536]], ['topbot', [9014]], ['topcir', [10993]], ['top', [8868]], ['Topf', [120139]], ['topf', [120165]], ['topfork', [10970]], ['tosa', [10537]], ['tprime', [8244]], ['trade', [8482]], ['TRADE', [8482]], ['triangle', [9653]], ['triangledown', [9663]], ['triangleleft', [9667]], ['trianglelefteq', [8884]], ['triangleq', [8796]], ['triangleright', [9657]], ['trianglerighteq', [8885]], ['tridot', [9708]], ['trie', [8796]], ['triminus', [10810]], ['TripleDot', [8411]], ['triplus', [10809]], ['trisb', [10701]], ['tritime', [10811]], ['trpezium', [9186]], ['Tscr', [119983]], ['tscr', [120009]], ['TScy', [1062]], ['tscy', [1094]], ['TSHcy', [1035]], ['tshcy', [1115]], ['Tstrok', [358]], ['tstrok', [359]], ['twixt', [8812]], ['twoheadleftarrow', [8606]], ['twoheadrightarrow', [8608]], ['Uacute', [218]], ['uacute', [250]], ['uarr', [8593]], ['Uarr', [8607]], ['uArr', [8657]], ['Uarrocir', [10569]], ['Ubrcy', [1038]], ['ubrcy', [1118]], ['Ubreve', [364]], ['ubreve', [365]], ['Ucirc', [219]], ['ucirc', [251]], ['Ucy', [1059]], ['ucy', [1091]], ['udarr', [8645]], ['Udblac', [368]], ['udblac', [369]], ['udhar', [10606]], ['ufisht', [10622]], ['Ufr', [120088]], ['ufr', [120114]], ['Ugrave', [217]], ['ugrave', [249]], ['uHar', [10595]], ['uharl', [8639]], ['uharr', [8638]], ['uhblk', [9600]], ['ulcorn', [8988]], ['ulcorner', [8988]], ['ulcrop', [8975]], ['ultri', [9720]], ['Umacr', [362]], ['umacr', [363]], ['uml', [168]], ['UnderBar', [95]], ['UnderBrace', [9183]], ['UnderBracket', [9141]], ['UnderParenthesis', [9181]], ['Union', [8899]], ['UnionPlus', [8846]], ['Uogon', [370]], ['uogon', [371]], ['Uopf', [120140]], ['uopf', [120166]], ['UpArrowBar', [10514]], ['uparrow', [8593]], ['UpArrow', [8593]], ['Uparrow', [8657]], ['UpArrowDownArrow', [8645]], ['updownarrow', [8597]], ['UpDownArrow', [8597]], ['Updownarrow', [8661]], ['UpEquilibrium', [10606]], ['upharpoonleft', [8639]], ['upharpoonright', [8638]], ['uplus', [8846]], ['UpperLeftArrow', [8598]], ['UpperRightArrow', [8599]], ['upsi', [965]], ['Upsi', [978]], ['upsih', [978]], ['Upsilon', [933]], ['upsilon', [965]], ['UpTeeArrow', [8613]], ['UpTee', [8869]], ['upuparrows', [8648]], ['urcorn', [8989]], ['urcorner', [8989]], ['urcrop', [8974]], ['Uring', [366]], ['uring', [367]], ['urtri', [9721]], ['Uscr', [119984]], ['uscr', [120010]], ['utdot', [8944]], ['Utilde', [360]], ['utilde', [361]], ['utri', [9653]], ['utrif', [9652]], ['uuarr', [8648]], ['Uuml', [220]], ['uuml', [252]], ['uwangle', [10663]], ['vangrt', [10652]], ['varepsilon', [1013]], ['varkappa', [1008]], ['varnothing', [8709]], ['varphi', [981]], ['varpi', [982]], ['varpropto', [8733]], ['varr', [8597]], ['vArr', [8661]], ['varrho', [1009]], ['varsigma', [962]], ['varsubsetneq', [8842, 65024]], ['varsubsetneqq', [10955, 65024]], ['varsupsetneq', [8843, 65024]], ['varsupsetneqq', [10956, 65024]], ['vartheta', [977]], ['vartriangleleft', [8882]], ['vartriangleright', [8883]], ['vBar', [10984]], ['Vbar', [10987]], ['vBarv', [10985]], ['Vcy', [1042]], ['vcy', [1074]], ['vdash', [8866]], ['vDash', [8872]], ['Vdash', [8873]], ['VDash', [8875]], ['Vdashl', [10982]], ['veebar', [8891]], ['vee', [8744]], ['Vee', [8897]], ['veeeq', [8794]], ['vellip', [8942]], ['verbar', [124]], ['Verbar', [8214]], ['vert', [124]], ['Vert', [8214]], ['VerticalBar', [8739]], ['VerticalLine', [124]], ['VerticalSeparator', [10072]], ['VerticalTilde', [8768]], ['VeryThinSpace', [8202]], ['Vfr', [120089]], ['vfr', [120115]], ['vltri', [8882]], ['vnsub', [8834, 8402]], ['vnsup', [8835, 8402]], ['Vopf', [120141]], ['vopf', [120167]], ['vprop', [8733]], ['vrtri', [8883]], ['Vscr', [119985]], ['vscr', [120011]], ['vsubnE', [10955, 65024]], ['vsubne', [8842, 65024]], ['vsupnE', [10956, 65024]], ['vsupne', [8843, 65024]], ['Vvdash', [8874]], ['vzigzag', [10650]], ['Wcirc', [372]], ['wcirc', [373]], ['wedbar', [10847]], ['wedge', [8743]], ['Wedge', [8896]], ['wedgeq', [8793]], ['weierp', [8472]], ['Wfr', [120090]], ['wfr', [120116]], ['Wopf', [120142]], ['wopf', [120168]], ['wp', [8472]], ['wr', [8768]], ['wreath', [8768]], ['Wscr', [119986]], ['wscr', [120012]], ['xcap', [8898]], ['xcirc', [9711]], ['xcup', [8899]], ['xdtri', [9661]], ['Xfr', [120091]], ['xfr', [120117]], ['xharr', [10231]], ['xhArr', [10234]], ['Xi', [926]], ['xi', [958]], ['xlarr', [10229]], ['xlArr', [10232]], ['xmap', [10236]], ['xnis', [8955]], ['xodot', [10752]], ['Xopf', [120143]], ['xopf', [120169]], ['xoplus', [10753]], ['xotime', [10754]], ['xrarr', [10230]], ['xrArr', [10233]], ['Xscr', [119987]], ['xscr', [120013]], ['xsqcup', [10758]], ['xuplus', [10756]], ['xutri', [9651]], ['xvee', [8897]], ['xwedge', [8896]], ['Yacute', [221]], ['yacute', [253]], ['YAcy', [1071]], ['yacy', [1103]], ['Ycirc', [374]], ['ycirc', [375]], ['Ycy', [1067]], ['ycy', [1099]], ['yen', [165]], ['Yfr', [120092]], ['yfr', [120118]], ['YIcy', [1031]], ['yicy', [1111]], ['Yopf', [120144]], ['yopf', [120170]], ['Yscr', [119988]], ['yscr', [120014]], ['YUcy', [1070]], ['yucy', [1102]], ['yuml', [255]], ['Yuml', [376]], ['Zacute', [377]], ['zacute', [378]], ['Zcaron', [381]], ['zcaron', [382]], ['Zcy', [1047]], ['zcy', [1079]], ['Zdot', [379]], ['zdot', [380]], ['zeetrf', [8488]], ['ZeroWidthSpace', [8203]], ['Zeta', [918]], ['zeta', [950]], ['zfr', [120119]], ['Zfr', [8488]], ['ZHcy', [1046]], ['zhcy', [1078]], ['zigrarr', [8669]], ['zopf', [120171]], ['Zopf', [8484]], ['Zscr', [119989]], ['zscr', [120015]], ['zwj', [8205]], ['zwnj', [8204]]];

var alphaIndex = {};
var charIndex = {};

createIndexes(alphaIndex, charIndex);

/**
 * @constructor
 */
function Html5Entities() {}

/**
 * @param {String} str
 * @returns {String}
 */
Html5Entities.prototype.decode = function(str) {
    if (!str || !str.length) {
        return '';
    }
    return str.replace(/&(#?[\w\d]+);?/g, function(s, entity) {
        var chr;
        if (entity.charAt(0) === "#") {
            var code = entity.charAt(1) === 'x' ?
                parseInt(entity.substr(2).toLowerCase(), 16) :
                parseInt(entity.substr(1));

            if (!(isNaN(code) || code < -32768 || code > 65535)) {
                chr = String.fromCharCode(code);
            }
        } else {
            chr = alphaIndex[entity];
        }
        return chr || s;
    });
};

/**
 * @param {String} str
 * @returns {String}
 */
 Html5Entities.decode = function(str) {
    return new Html5Entities().decode(str);
 };

/**
 * @param {String} str
 * @returns {String}
 */
Html5Entities.prototype.encode = function(str) {
    if (!str || !str.length) {
        return '';
    }
    var strLength = str.length;
    var result = '';
    var i = 0;
    while (i < strLength) {
        var charInfo = charIndex[str.charCodeAt(i)];
        if (charInfo) {
            var alpha = charInfo[str.charCodeAt(i + 1)];
            if (alpha) {
                i++;
            } else {
                alpha = charInfo[''];
            }
            if (alpha) {
                result += "&" + alpha + ";";
                i++;
                continue;
            }
        }
        result += str.charAt(i);
        i++;
    }
    return result;
};

/**
 * @param {String} str
 * @returns {String}
 */
 Html5Entities.encode = function(str) {
    return new Html5Entities().encode(str);
 };

/**
 * @param {String} str
 * @returns {String}
 */
Html5Entities.prototype.encodeNonUTF = function(str) {
    if (!str || !str.length) {
        return '';
    }
    var strLength = str.length;
    var result = '';
    var i = 0;
    while (i < strLength) {
        var c = str.charCodeAt(i);
        var charInfo = charIndex[c];
        if (charInfo) {
            var alpha = charInfo[str.charCodeAt(i + 1)];
            if (alpha) {
                i++;
            } else {
                alpha = charInfo[''];
            }
            if (alpha) {
                result += "&" + alpha + ";";
                i++;
                continue;
            }
        }
        if (c < 32 || c > 126) {
            result += '&#' + c + ';';
        } else {
            result += str.charAt(i);
        }
        i++;
    }
    return result;
};

/**
 * @param {String} str
 * @returns {String}
 */
 Html5Entities.encodeNonUTF = function(str) {
    return new Html5Entities().encodeNonUTF(str);
 };

/**
 * @param {String} str
 * @returns {String}
 */
Html5Entities.prototype.encodeNonASCII = function(str) {
    if (!str || !str.length) {
        return '';
    }
    var strLength = str.length;
    var result = '';
    var i = 0;
    while (i < strLength) {
        var c = str.charCodeAt(i);
        if (c <= 255) {
            result += str[i++];
            continue;
        }
        result += '&#' + c + ';';
        i++
    }
    return result;
};

/**
 * @param {String} str
 * @returns {String}
 */
 Html5Entities.encodeNonASCII = function(str) {
    return new Html5Entities().encodeNonASCII(str);
 };

/**
 * @param {Object} alphaIndex Passed by reference.
 * @param {Object} charIndex Passed by reference.
 */
function createIndexes(alphaIndex, charIndex) {
    var i = ENTITIES.length;
    var _results = [];
    while (i--) {
        var e = ENTITIES[i];
        var alpha = e[0];
        var chars = e[1];
        var chr = chars[0];
        var addChar = (chr < 32 || chr > 126) || chr === 62 || chr === 60 || chr === 38 || chr === 34 || chr === 39;
        var charInfo;
        if (addChar) {
            charInfo = charIndex[chr] = charIndex[chr] || {};
        }
        if (chars[1]) {
            var chr2 = chars[1];
            alphaIndex[alpha] = String.fromCharCode(chr) + String.fromCharCode(chr2);
            _results.push(addChar && (charInfo[chr2] = alpha));
        } else {
            alphaIndex[alpha] = String.fromCharCode(chr);
            _results.push(addChar && (charInfo[''] = alpha));
        }
    }
}

module.exports = Html5Entities;


/***/ }),
/* 8 */
/***/ (function(module, __webpack_exports__, __webpack_require__) {

"use strict";
Object.defineProperty(__webpack_exports__, "__esModule", { value: true });
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_0_reflect_metadata__ = __webpack_require__(43);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_0_reflect_metadata___default = __webpack_require__.n(__WEBPACK_IMPORTED_MODULE_0_reflect_metadata__);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_1_zone_js__ = __webpack_require__(59);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_1_zone_js___default = __webpack_require__.n(__WEBPACK_IMPORTED_MODULE_1_zone_js__);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_2_bootstrap__ = __webpack_require__(58);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_2_bootstrap___default = __webpack_require__.n(__WEBPACK_IMPORTED_MODULE_2_bootstrap__);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_3__angular_core__ = __webpack_require__(1);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_4__angular_platform_browser_dynamic__ = __webpack_require__(56);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_5__app_app_browser_module__ = __webpack_require__(13);






//if ((params.data != null) && ('sqWebAppName' in params.data) && (params.data.sqWebAppName != null) && (params.data.sqWebAppName == "HealthMonitorWebApp")) {
//}
if (true) {
    module.hot.accept();
    module.hot.dispose(function () {
        // Before restarting the app, we create a new root element and dispose the old one
        var oldRootElem = document.querySelector('app');
        var newRootElem = document.createElement('app');
        oldRootElem.parentNode.insertBefore(newRootElem, oldRootElem);
        modulePromise.then(function (appModule) { return appModule.destroy(); });
    });
}
else {
    enableProdMode();
}
// Note: @ng-tools/webpack looks for the following expression when performing production
// builds. Don't change how this line looks, otherwise you may break tree-shaking.
var modulePromise = __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_4__angular_platform_browser_dynamic__["platformBrowserDynamic"])().bootstrapModule(__WEBPACK_IMPORTED_MODULE_5__app_app_browser_module__["a" /* AppModule */]);


/***/ }),
/* 9 */
/***/ (function(module, exports, __webpack_require__) {

/* WEBPACK VAR INJECTION */(function(__resourceQuery, module) {/*eslint-env browser*/
/*global __resourceQuery __webpack_public_path__*/

var options = {
  path: "/__webpack_hmr",
  timeout: 20 * 1000,
  overlay: true,
  reload: false,
  log: true,
  warn: true,
  name: ''
};
if (true) {
  var querystring = __webpack_require__(42);
  var overrides = querystring.parse(__resourceQuery.slice(1));
  if (overrides.path) options.path = overrides.path;
  if (overrides.timeout) options.timeout = overrides.timeout;
  if (overrides.overlay) options.overlay = overrides.overlay !== 'false';
  if (overrides.reload) options.reload = overrides.reload !== 'false';
  if (overrides.noInfo && overrides.noInfo !== 'false') {
    options.log = false;
  }
  if (overrides.name) {
    options.name = overrides.name;
  }
  if (overrides.quiet && overrides.quiet !== 'false') {
    options.log = false;
    options.warn = false;
  }
  if (overrides.dynamicPublicPath) {
    options.path = __webpack_require__.p + options.path;
  }
}

if (typeof window === 'undefined') {
  // do nothing
} else if (typeof window.EventSource === 'undefined') {
  console.warn(
    "webpack-hot-middleware's client requires EventSource to work. " +
    "You should include a polyfill if you want to support this browser: " +
    "https://developer.mozilla.org/en-US/docs/Web/API/Server-sent_events#Tools"
  );
} else {
  connect();
}

function EventSourceWrapper() {
  var source;
  var lastActivity = new Date();
  var listeners = [];

  init();
  var timer = setInterval(function() {
    if ((new Date() - lastActivity) > options.timeout) {
      handleDisconnect();
    }
  }, options.timeout / 2);

  function init() {
    source = new window.EventSource(options.path);
    source.onopen = handleOnline;
    source.onerror = handleDisconnect;
    source.onmessage = handleMessage;
  }

  function handleOnline() {
    if (options.log) console.log("[HMR] connected");
    lastActivity = new Date();
  }

  function handleMessage(event) {
    lastActivity = new Date();
    for (var i = 0; i < listeners.length; i++) {
      listeners[i](event);
    }
  }

  function handleDisconnect() {
    clearInterval(timer);
    source.close();
    setTimeout(init, options.timeout);
  }

  return {
    addMessageListener: function(fn) {
      listeners.push(fn);
    }
  };
}

function getEventSourceWrapper() {
  if (!window.__whmEventSourceWrapper) {
    window.__whmEventSourceWrapper = {};
  }
  if (!window.__whmEventSourceWrapper[options.path]) {
    // cache the wrapper for other entries loaded on
    // the same page with the same options.path
    window.__whmEventSourceWrapper[options.path] = EventSourceWrapper();
  }
  return window.__whmEventSourceWrapper[options.path];
}

function connect() {
  getEventSourceWrapper().addMessageListener(handleMessage);

  function handleMessage(event) {
    if (event.data == "\uD83D\uDC93") {
      return;
    }
    try {
      processMessage(JSON.parse(event.data));
    } catch (ex) {
      if (options.warn) {
        console.warn("Invalid HMR message: " + event.data + "\n" + ex);
      }
    }
  }
}

// the reporter needs to be a singleton on the page
// in case the client is being used by multiple bundles
// we only want to report once.
// all the errors will go to all clients
var singletonKey = '__webpack_hot_middleware_reporter__';
var reporter;
if (typeof window !== 'undefined') {
  if (!window[singletonKey]) {
    window[singletonKey] = createReporter();
  }
  reporter = window[singletonKey];
}

function createReporter() {
  var strip = __webpack_require__(44);

  var overlay;
  if (typeof document !== 'undefined' && options.overlay) {
    overlay = __webpack_require__(49);
  }

  var styles = {
    errors: "color: #ff0000;",
    warnings: "color: #999933;"
  };
  var previousProblems = null;
  function log(type, obj) {
    var newProblems = obj[type].map(function(msg) { return strip(msg); }).join('\n');
    if (previousProblems == newProblems) {
      return;
    } else {
      previousProblems = newProblems;
    }

    var style = styles[type];
    var name = obj.name ? "'" + obj.name + "' " : "";
    var title = "[HMR] bundle " + name + "has " + obj[type].length + " " + type;
    // NOTE: console.warn or console.error will print the stack trace
    // which isn't helpful here, so using console.log to escape it.
    if (console.group && console.groupEnd) {
      console.group("%c" + title, style);
      console.log("%c" + newProblems, style);
      console.groupEnd();
    } else {
      console.log(
        "%c" + title + "\n\t%c" + newProblems.replace(/\n/g, "\n\t"),
        style + "font-weight: bold;",
        style + "font-weight: normal;"
      );
    }
  }

  return {
    cleanProblemsCache: function () {
      previousProblems = null;
    },
    problems: function(type, obj) {
      if (options.warn) {
        log(type, obj);
      }
      if (overlay && type !== 'warnings') overlay.showProblems(type, obj[type]);
    },
    success: function() {
      if (overlay) overlay.clear();
    },
    useCustomOverlay: function(customOverlay) {
      overlay = customOverlay;
    }
  };
}

var processUpdate = __webpack_require__(50);

var customHandler;
var subscribeAllHandler;
function processMessage(obj) {
  switch(obj.action) {
    case "building":
      if (options.log) {
        console.log(
          "[HMR] bundle " + (obj.name ? "'" + obj.name + "' " : "") +
          "rebuilding"
        );
      }
      break;
    case "built":
      if (options.log) {
        console.log(
          "[HMR] bundle " + (obj.name ? "'" + obj.name + "' " : "") +
          "rebuilt in " + obj.time + "ms"
        );
      }
      // fall through
    case "sync":
      if (obj.name && options.name && obj.name !== options.name) {
        return;
      }
      if (obj.errors.length > 0) {
        if (reporter) reporter.problems('errors', obj);
      } else {
        if (reporter) {
          if (obj.warnings.length > 0) {
            reporter.problems('warnings', obj);
          } else {
            reporter.cleanProblemsCache();
          }
          reporter.success();
        }
        processUpdate(obj.hash, obj.modules, options);
      }
      break;
    default:
      if (customHandler) {
        customHandler(obj);
      }
  }

  if (subscribeAllHandler) {
    subscribeAllHandler(obj);
  }
}

if (module) {
  module.exports = {
    subscribeAll: function subscribeAll(handler) {
      subscribeAllHandler = handler;
    },
    subscribe: function subscribe(handler) {
      customHandler = handler;
    },
    useCustomOverlay: function useCustomOverlay(customOverlay) {
      if (reporter) reporter.useCustomOverlay(customOverlay);
    }
  };
}

/* WEBPACK VAR INJECTION */}.call(exports, "?path=__webpack_hmr&dynamicPublicPath=true", __webpack_require__(51)(module)))

/***/ }),
/* 10 */
/***/ (function(module, exports, __webpack_require__) {

module.exports = (__webpack_require__(0))(45);

/***/ }),
/* 11 */
/***/ (function(module, exports, __webpack_require__) {

"use strict";


module.exports = ansiHTML

// Reference to https://github.com/sindresorhus/ansi-regex
var _regANSI = /(?:(?:\u001b\[)|\u009b)(?:(?:[0-9]{1,3})?(?:(?:;[0-9]{0,3})*)?[A-M|f-m])|\u001b[A-M]/

var _defColors = {
  reset: ['fff', '000'], // [FOREGROUD_COLOR, BACKGROUND_COLOR]
  black: '000',
  red: 'ff0000',
  green: '209805',
  yellow: 'e8bf03',
  blue: '0000ff',
  magenta: 'ff00ff',
  cyan: '00ffee',
  lightgrey: 'f0f0f0',
  darkgrey: '888'
}
var _styles = {
  30: 'black',
  31: 'red',
  32: 'green',
  33: 'yellow',
  34: 'blue',
  35: 'magenta',
  36: 'cyan',
  37: 'lightgrey'
}
var _openTags = {
  '1': 'font-weight:bold', // bold
  '2': 'opacity:0.5', // dim
  '3': '<i>', // italic
  '4': '<u>', // underscore
  '8': 'display:none', // hidden
  '9': '<del>' // delete
}
var _closeTags = {
  '23': '</i>', // reset italic
  '24': '</u>', // reset underscore
  '29': '</del>' // reset delete
}

;[0, 21, 22, 27, 28, 39, 49].forEach(function (n) {
  _closeTags[n] = '</span>'
})

/**
 * Converts text with ANSI color codes to HTML markup.
 * @param {String} text
 * @returns {*}
 */
function ansiHTML (text) {
  // Returns the text if the string has no ANSI escape code.
  if (!_regANSI.test(text)) {
    return text
  }

  // Cache opened sequence.
  var ansiCodes = []
  // Replace with markup.
  var ret = text.replace(/\033\[(\d+)*m/g, function (match, seq) {
    var ot = _openTags[seq]
    if (ot) {
      // If current sequence has been opened, close it.
      if (!!~ansiCodes.indexOf(seq)) { // eslint-disable-line no-extra-boolean-cast
        ansiCodes.pop()
        return '</span>'
      }
      // Open tag.
      ansiCodes.push(seq)
      return ot[0] === '<' ? ot : '<span style="' + ot + ';">'
    }

    var ct = _closeTags[seq]
    if (ct) {
      // Pop sequence
      ansiCodes.pop()
      return ct
    }
    return ''
  })

  // Make sure tags are closed.
  var l = ansiCodes.length
  ;(l > 0) && (ret += Array(l + 1).join('</span>'))

  return ret
}

/**
 * Customize colors.
 * @param {Object} colors reference to _defColors
 */
ansiHTML.setColors = function (colors) {
  if (typeof colors !== 'object') {
    throw new Error('`colors` parameter must be an Object.')
  }

  var _finalColors = {}
  for (var key in _defColors) {
    var hex = colors.hasOwnProperty(key) ? colors[key] : null
    if (!hex) {
      _finalColors[key] = _defColors[key]
      continue
    }
    if ('reset' === key) {
      if (typeof hex === 'string') {
        hex = [hex]
      }
      if (!Array.isArray(hex) || hex.length === 0 || hex.some(function (h) {
        return typeof h !== 'string'
      })) {
        throw new Error('The value of `' + key + '` property must be an Array and each item could only be a hex string, e.g.: FF0000')
      }
      var defHexColor = _defColors[key]
      if (!hex[0]) {
        hex[0] = defHexColor[0]
      }
      if (hex.length === 1 || !hex[1]) {
        hex = [hex[0]]
        hex.push(defHexColor[1])
      }

      hex = hex.slice(0, 2)
    } else if (typeof hex !== 'string') {
      throw new Error('The value of `' + key + '` property must be a hex string, e.g.: FF0000')
    }
    _finalColors[key] = hex
  }
  _setTags(_finalColors)
}

/**
 * Reset colors.
 */
ansiHTML.reset = function () {
  _setTags(_defColors)
}

/**
 * Expose tags, including open and close.
 * @type {Object}
 */
ansiHTML.tags = {}

if (Object.defineProperty) {
  Object.defineProperty(ansiHTML.tags, 'open', {
    get: function () { return _openTags }
  })
  Object.defineProperty(ansiHTML.tags, 'close', {
    get: function () { return _closeTags }
  })
} else {
  ansiHTML.tags.open = _openTags
  ansiHTML.tags.close = _closeTags
}

function _setTags (colors) {
  // reset all
  _openTags['0'] = 'font-weight:normal;opacity:1;color:#' + colors.reset[0] + ';background:#' + colors.reset[1]
  // inverse
  _openTags['7'] = 'color:#' + colors.reset[1] + ';background:#' + colors.reset[0]
  // dark grey
  _openTags['90'] = 'color:#' + colors.darkgrey

  for (var code in _styles) {
    var color = _styles[code]
    var oriColor = colors[color] || '000'
    _openTags[code] = 'color:#' + oriColor
    code = parseInt(code)
    _openTags[(code + 10).toString()] = 'background:#' + oriColor
  }
}

ansiHTML.reset()


/***/ }),
/* 12 */
/***/ (function(module, exports, __webpack_require__) {

"use strict";

module.exports = function () {
	return /[\u001b\u009b][[()#;?]*(?:[0-9]{1,4}(?:;[0-9]{0,4})*)?[0-9A-PRZcf-nqry=><]/g;
};


/***/ }),
/* 13 */
/***/ (function(module, __webpack_exports__, __webpack_require__) {

"use strict";
/* harmony export (binding) */ __webpack_require__.d(__webpack_exports__, "a", function() { return AppModule; });
/* unused harmony export getBaseUrl */
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_0__angular_core__ = __webpack_require__(1);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_1__angular_platform_browser__ = __webpack_require__(60);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_2__app_shared_module__ = __webpack_require__(14);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_3__components_app_app_component__ = __webpack_require__(6);
var __decorate = (this && this.__decorate) || function (decorators, target, key, desc) {
    var c = arguments.length, r = c < 3 ? target : desc === null ? desc = Object.getOwnPropertyDescriptor(target, key) : desc, d;
    if (typeof Reflect === "object" && typeof Reflect.decorate === "function") r = Reflect.decorate(decorators, target, key, desc);
    else for (var i = decorators.length - 1; i >= 0; i--) if (d = decorators[i]) r = (c < 3 ? d(r) : c > 3 ? d(target, key, r) : d(target, key)) || r;
    return c > 3 && r && Object.defineProperty(target, key, r), r;
};




var AppModule = (function () {
    function AppModule() {
    }
    AppModule = __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["NgModule"])({
            bootstrap: [__WEBPACK_IMPORTED_MODULE_3__components_app_app_component__["a" /* AppComponent */]],
            imports: [
                __WEBPACK_IMPORTED_MODULE_1__angular_platform_browser__["BrowserModule"],
                __WEBPACK_IMPORTED_MODULE_2__app_shared_module__["a" /* AppModuleShared */]
            ],
            providers: [
                { provide: 'BASE_URL', useFactory: getBaseUrl }
            ]
        })
    ], AppModule);
    return AppModule;
}());

function getBaseUrl() {
    return document.getElementsByTagName('base')[0].href;
}


/***/ }),
/* 14 */
/***/ (function(module, __webpack_exports__, __webpack_require__) {

"use strict";
/* harmony export (binding) */ __webpack_require__.d(__webpack_exports__, "a", function() { return AppModuleShared; });
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_0__angular_core__ = __webpack_require__(1);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_1__angular_common__ = __webpack_require__(62);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_2__angular_forms__ = __webpack_require__(55);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_3__angular_http__ = __webpack_require__(5);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_4__angular_router__ = __webpack_require__(57);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_5__components_app_app_component__ = __webpack_require__(6);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_6__components_navmenu_navmenu_component__ = __webpack_require__(19);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_7__components_home_home_component__ = __webpack_require__(18);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_8__components_fetchdata_fetchdata_component__ = __webpack_require__(16);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_9__components_counter_counter_component__ = __webpack_require__(15);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_10__components_healthmonitor_healthmonitor_component__ = __webpack_require__(17);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_11__components_quicktester_quicktester_component__ = __webpack_require__(25);
var __decorate = (this && this.__decorate) || function (decorators, target, key, desc) {
    var c = arguments.length, r = c < 3 ? target : desc === null ? desc = Object.getOwnPropertyDescriptor(target, key) : desc, d;
    if (typeof Reflect === "object" && typeof Reflect.decorate === "function") r = Reflect.decorate(decorators, target, key, desc);
    else for (var i = decorators.length - 1; i >= 0; i--) if (d = decorators[i]) r = (c < 3 ? d(r) : c > 3 ? d(target, key, r) : d(target, key)) || r;
    return c > 3 && r && Object.defineProperty(target, key, r), r;
};












var AppModuleShared = (function () {
    function AppModuleShared() {
    }
    AppModuleShared = __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["NgModule"])({
            declarations: [
                __WEBPACK_IMPORTED_MODULE_5__components_app_app_component__["a" /* AppComponent */],
                __WEBPACK_IMPORTED_MODULE_6__components_navmenu_navmenu_component__["a" /* NavMenuComponent */],
                __WEBPACK_IMPORTED_MODULE_9__components_counter_counter_component__["a" /* CounterComponent */],
                __WEBPACK_IMPORTED_MODULE_8__components_fetchdata_fetchdata_component__["a" /* FetchDataComponent */],
                __WEBPACK_IMPORTED_MODULE_10__components_healthmonitor_healthmonitor_component__["a" /* HealthMonitorComponent */],
                __WEBPACK_IMPORTED_MODULE_11__components_quicktester_quicktester_component__["a" /* QuickTesterComponent */],
                __WEBPACK_IMPORTED_MODULE_7__components_home_home_component__["a" /* HomeComponent */]
            ],
            imports: [
                __WEBPACK_IMPORTED_MODULE_1__angular_common__["CommonModule"],
                __WEBPACK_IMPORTED_MODULE_3__angular_http__["HttpModule"],
                __WEBPACK_IMPORTED_MODULE_2__angular_forms__["FormsModule"],
                __WEBPACK_IMPORTED_MODULE_4__angular_router__["RouterModule"].forRoot([
                    { path: '', redirectTo: 'home', pathMatch: 'full' },
                    { path: 'home', component: __WEBPACK_IMPORTED_MODULE_7__components_home_home_component__["a" /* HomeComponent */] },
                    { path: 'counter', component: __WEBPACK_IMPORTED_MODULE_9__components_counter_counter_component__["a" /* CounterComponent */] },
                    { path: 'healthmonitor', component: __WEBPACK_IMPORTED_MODULE_10__components_healthmonitor_healthmonitor_component__["a" /* HealthMonitorComponent */] },
                    { path: 'HealthMonitor', component: __WEBPACK_IMPORTED_MODULE_10__components_healthmonitor_healthmonitor_component__["a" /* HealthMonitorComponent */] },
                    { path: 'quicktester', component: __WEBPACK_IMPORTED_MODULE_11__components_quicktester_quicktester_component__["a" /* QuickTesterComponent */] },
                    { path: 'QuickTester', component: __WEBPACK_IMPORTED_MODULE_11__components_quicktester_quicktester_component__["a" /* QuickTesterComponent */] },
                    { path: 'fetch-data', component: __WEBPACK_IMPORTED_MODULE_8__components_fetchdata_fetchdata_component__["a" /* FetchDataComponent */] },
                    { path: '**', redirectTo: 'home' }
                ])
            ]
        })
    ], AppModuleShared);
    return AppModuleShared;
}());



/***/ }),
/* 15 */
/***/ (function(module, __webpack_exports__, __webpack_require__) {

"use strict";
/* harmony export (binding) */ __webpack_require__.d(__webpack_exports__, "a", function() { return CounterComponent; });
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_0__angular_core__ = __webpack_require__(1);
var __decorate = (this && this.__decorate) || function (decorators, target, key, desc) {
    var c = arguments.length, r = c < 3 ? target : desc === null ? desc = Object.getOwnPropertyDescriptor(target, key) : desc, d;
    if (typeof Reflect === "object" && typeof Reflect.decorate === "function") r = Reflect.decorate(decorators, target, key, desc);
    else for (var i = decorators.length - 1; i >= 0; i--) if (d = decorators[i]) r = (c < 3 ? d(r) : c > 3 ? d(target, key, r) : d(target, key)) || r;
    return c > 3 && r && Object.defineProperty(target, key, r), r;
};

var CounterComponent = (function () {
    function CounterComponent() {
        this.currentCount = 0;
    }
    CounterComponent.prototype.incrementCounter = function () {
        this.currentCount++;
    };
    CounterComponent = __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Component"])({
            selector: 'counter',
            template: __webpack_require__(34)
        })
    ], CounterComponent);
    return CounterComponent;
}());



/***/ }),
/* 16 */
/***/ (function(module, __webpack_exports__, __webpack_require__) {

"use strict";
/* harmony export (binding) */ __webpack_require__.d(__webpack_exports__, "a", function() { return FetchDataComponent; });
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_0__angular_core__ = __webpack_require__(1);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_1__angular_http__ = __webpack_require__(5);
var __decorate = (this && this.__decorate) || function (decorators, target, key, desc) {
    var c = arguments.length, r = c < 3 ? target : desc === null ? desc = Object.getOwnPropertyDescriptor(target, key) : desc, d;
    if (typeof Reflect === "object" && typeof Reflect.decorate === "function") r = Reflect.decorate(decorators, target, key, desc);
    else for (var i = decorators.length - 1; i >= 0; i--) if (d = decorators[i]) r = (c < 3 ? d(r) : c > 3 ? d(target, key, r) : d(target, key)) || r;
    return c > 3 && r && Object.defineProperty(target, key, r), r;
};
var __metadata = (this && this.__metadata) || function (k, v) {
    if (typeof Reflect === "object" && typeof Reflect.metadata === "function") return Reflect.metadata(k, v);
};
var __param = (this && this.__param) || function (paramIndex, decorator) {
    return function (target, key) { decorator(target, key, paramIndex); }
};


var FetchDataComponent = (function () {
    function FetchDataComponent(http, baseUrl) {
        var _this = this;
        http.get(baseUrl + 'api/SampleData/WeatherForecasts').subscribe(function (result) {
            _this.forecasts = result.json();
        }, function (error) { return console.error(error); });
    }
    FetchDataComponent = __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Component"])({
            selector: 'fetchdata',
            template: __webpack_require__(35)
        }),
        __param(1, __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Inject"])('BASE_URL')),
        __metadata("design:paramtypes", [__WEBPACK_IMPORTED_MODULE_1__angular_http__["Http"], String])
    ], FetchDataComponent);
    return FetchDataComponent;
}());



/***/ }),
/* 17 */
/***/ (function(module, __webpack_exports__, __webpack_require__) {

"use strict";
/* harmony export (binding) */ __webpack_require__.d(__webpack_exports__, "a", function() { return HealthMonitorComponent; });
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_0__angular_core__ = __webpack_require__(1);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_1__angular_http__ = __webpack_require__(5);
var __decorate = (this && this.__decorate) || function (decorators, target, key, desc) {
    var c = arguments.length, r = c < 3 ? target : desc === null ? desc = Object.getOwnPropertyDescriptor(target, key) : desc, d;
    if (typeof Reflect === "object" && typeof Reflect.decorate === "function") r = Reflect.decorate(decorators, target, key, desc);
    else for (var i = decorators.length - 1; i >= 0; i--) if (d = decorators[i]) r = (c < 3 ? d(r) : c > 3 ? d(target, key, r) : d(target, key)) || r;
    return c > 3 && r && Object.defineProperty(target, key, r), r;
};
var __metadata = (this && this.__metadata) || function (k, v) {
    if (typeof Reflect === "object" && typeof Reflect.metadata === "function") return Reflect.metadata(k, v);
};
var __param = (this && this.__param) || function (paramIndex, decorator) {
    return function (target, key) { decorator(target, key, paramIndex); }
};


var gDefaultHMData = {
    AppOk: "OK",
    StartDate: '1998-11-16T00:00:00',
    StartDateLoc: new Date('1998-11-16T00:00:00'),
    StartDateTimeSpanStr: '',
    DailyEmailReportEnabled: false,
    RtpsOk: 'OK',
    RtpsTimerEnabled: false,
    RtpsTimerFrequencyMinutes: -999,
    RtpsDownloads: ['a', 'b'],
    VBrokerOk: 'OK',
    ProcessingVBrokerMessagesEnabled: false,
    VBrokerReports: ['a', 'b'],
    VBrokerDetailedReports: ['a', 'b'],
    CommandToBackEnd: "OnlyGetData",
    ResponseToFrontEnd: "OK"
};
var HealthMonitorComponent = (function () {
    function HealthMonitorComponent(http, baseUrl) {
        this.m_title = 'SQ HealthMonitor Dashboard'; // strongly typed variables in TS
        this.m_userEmail = 'Unknown user';
        this.m_http = http;
        this.m_baseUrl = baseUrl;
        this.getHMData(gDefaultHMData);
        if (typeof gSqUserEmail == "undefined")
            this.m_userEmail = 'undefined@gmail.com';
        else
            this.m_userEmail = gSqUserEmail;
    }
    HealthMonitorComponent.prototype.getHMData = function (p_hmDataToSend) {
        var _this = this;
        console.log("getHMData().http with Post start");
        this.m_http.post(this.m_baseUrl + 'WebServer/ReportHealthMonitorCurrentStateToDashboardInJSON', p_hmDataToSend).subscribe(function (result) {
            console.log("getHMData().http.post('ReportHealthMonitorCurrentStateToDashboardInJSON') returned.");
            console.log("getHMData().http post returned answer: " + JSON.stringify(result));
            //var hmData: HMData = <HMData>hmDataReturned; // Typescript cast: remember that this is a compile-time cast, and not a runtime cast.
            var hmData = result.json(); // Typescript cast: remember that this is a compile-time cast, and not a runtime cast.
            _this.m_data = hmData;
            // Sadly Javascript loves Local time, so work in Local time; easier;
            // 1. StartDate
            _this.m_data.StartDateLoc = new Date(hmData.StartDate); // "2015-12-29 00:49:54.000Z", because of the Z Zero, this UTC string is converted properly to local time
            _this.m_data.StartDate = localDateToUtcString_yyyy_mm_dd_hh_mm_ss(_this.m_data.StartDateLoc); // take away the miliseconds from the dateStr
            var localNow = new Date(); // this is local time: <checked>
            //var utcNowGetTime = new Date().getTime();  //getTime() returns the number of seconds in UTC.
            _this.m_data.StartDateTimeSpanStr = getTimeSpanStr(_this.m_data.StartDateLoc, localNow);
            //this.m_data.ResponseToFrontEnd = "ERROR";
            _this.m_data.AppOk = 'OK';
            if (_this.m_data.ResponseToFrontEnd.toUpperCase().indexOf('ERROR') >= 0)
                _this.m_data.AppOk = 'ERROR';
            _this.m_data.RtpsOk = 'OK';
            for (var i in _this.m_data.RtpsDownloads) {
                if (_this.m_data.RtpsDownloads[i].indexOf('OK') >= 0) {
                    continue;
                }
                _this.m_data.RtpsOk = 'ERROR';
            }
            _this.m_data.VBrokerOk = 'OK';
            for (var i in _this.m_data.VBrokerReports) {
                if (_this.m_data.VBrokerReports[i].indexOf('OK') >= 0) {
                    continue;
                }
                _this.m_data.VBrokerOk = 'ERROR';
            }
            _this.m_webAppResponse = JSON.stringify(hmData);
        }, function (error) { return console.error('getHMData().There was an error: ' + error); });
    };
    HealthMonitorComponent.prototype.setControlValue = function (controlName, value) {
        console.log("setControlValue():" + controlName + "/" + value);
        console.log("setControlValue():" + controlName + "/" + value + "/" + this.m_data.DailyEmailReportEnabled);
        if (controlName == 'chkDailyEmail') {
            if (this.m_data.DailyEmailReportEnabled != value) {
                this.m_data.DailyEmailReportEnabled = value;
                this.m_data.CommandToBackEnd = "ApplyTheDifferences";
                this.getHMData(this.m_data);
            }
        }
        else if (controlName == 'chkRtps') {
            if (this.m_data.RtpsTimerEnabled != value) {
                this.m_data.RtpsTimerEnabled = value;
                this.m_data.CommandToBackEnd = "ApplyTheDifferences";
                this.getHMData(this.m_data);
            }
        }
        else if (controlName == 'chkVBroker') {
            if (this.m_data.ProcessingVBrokerMessagesEnabled != value) {
                this.m_data.ProcessingVBrokerMessagesEnabled = value;
                this.m_data.CommandToBackEnd = "ApplyTheDifferences";
                this.getHMData(this.m_data);
            }
        }
    };
    HealthMonitorComponent.prototype.refreshDataClicked = function () {
        console.log("refreshDataClicked");
        this.m_wasRefreshClicked = "refreshDataClicked";
        this.m_data.CommandToBackEnd = "OnlyGetData";
        this.getHMData(this.m_data);
    };
    HealthMonitorComponent = __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Component"])({
            selector: 'healthmonitor',
            template: __webpack_require__(36),
            styles: [__webpack_require__(46)]
        }),
        __param(1, __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Inject"])('BASE_URL')),
        __metadata("design:paramtypes", [__WEBPACK_IMPORTED_MODULE_1__angular_http__["Http"], String])
    ], HealthMonitorComponent);
    return HealthMonitorComponent;
}());

// ************** Utils section
function localDateToUtcString_yyyy_mm_dd_hh_mm_ss(p_date) {
    var year = "" + p_date.getUTCFullYear();
    var month = "" + (p_date.getUTCMonth() + 1);
    if (month.length == 1) {
        month = "0" + month;
    }
    var day = "" + p_date.getUTCDate();
    if (day.length == 1) {
        day = "0" + day;
    }
    var hour = "" + p_date.getUTCHours();
    if (hour.length == 1) {
        hour = "0" + hour;
    }
    var minute = "" + p_date.getUTCMinutes();
    if (minute.length == 1) {
        minute = "0" + minute;
    }
    var second = "" + p_date.getUTCSeconds();
    if (second.length == 1) {
        second = "0" + second;
    }
    return year + "-" + month + "-" + day + " " + hour + ":" + minute + ":" + second;
}
// Started on 2015-12-23 00:44 (0days 0h 12m ago)
function getTimeSpanStr(date1, date2) {
    var diff = date2.getTime() - date1.getTime();
    var days = Math.floor(diff / (1000 * 60 * 60 * 24));
    diff -= days * (1000 * 60 * 60 * 24);
    var hours = Math.floor(diff / (1000 * 60 * 60));
    diff -= hours * (1000 * 60 * 60);
    var mins = Math.floor(diff / (1000 * 60));
    diff -= mins * (1000 * 60);
    var seconds = Math.floor(diff / (1000));
    diff -= seconds * (1000);
    return "(" + days + "days " + hours + "h " + mins + "m " + seconds + "s ago)";
}


/***/ }),
/* 18 */
/***/ (function(module, __webpack_exports__, __webpack_require__) {

"use strict";
/* harmony export (binding) */ __webpack_require__.d(__webpack_exports__, "a", function() { return HomeComponent; });
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_0__angular_core__ = __webpack_require__(1);
var __decorate = (this && this.__decorate) || function (decorators, target, key, desc) {
    var c = arguments.length, r = c < 3 ? target : desc === null ? desc = Object.getOwnPropertyDescriptor(target, key) : desc, d;
    if (typeof Reflect === "object" && typeof Reflect.decorate === "function") r = Reflect.decorate(decorators, target, key, desc);
    else for (var i = decorators.length - 1; i >= 0; i--) if (d = decorators[i]) r = (c < 3 ? d(r) : c > 3 ? d(target, key, r) : d(target, key)) || r;
    return c > 3 && r && Object.defineProperty(target, key, r), r;
};

var HomeComponent = (function () {
    function HomeComponent() {
    }
    HomeComponent = __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Component"])({
            selector: 'home',
            template: __webpack_require__(37)
        })
    ], HomeComponent);
    return HomeComponent;
}());



/***/ }),
/* 19 */
/***/ (function(module, __webpack_exports__, __webpack_require__) {

"use strict";
/* harmony export (binding) */ __webpack_require__.d(__webpack_exports__, "a", function() { return NavMenuComponent; });
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_0__angular_core__ = __webpack_require__(1);
var __decorate = (this && this.__decorate) || function (decorators, target, key, desc) {
    var c = arguments.length, r = c < 3 ? target : desc === null ? desc = Object.getOwnPropertyDescriptor(target, key) : desc, d;
    if (typeof Reflect === "object" && typeof Reflect.decorate === "function") r = Reflect.decorate(decorators, target, key, desc);
    else for (var i = decorators.length - 1; i >= 0; i--) if (d = decorators[i]) r = (c < 3 ? d(r) : c > 3 ? d(target, key, r) : d(target, key)) || r;
    return c > 3 && r && Object.defineProperty(target, key, r), r;
};

var NavMenuComponent = (function () {
    function NavMenuComponent() {
    }
    NavMenuComponent = __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Component"])({
            selector: 'nav-menu',
            template: __webpack_require__(38),
            styles: [__webpack_require__(47)]
        })
    ], NavMenuComponent);
    return NavMenuComponent;
}());



/***/ }),
/* 20 */
/***/ (function(module, __webpack_exports__, __webpack_require__) {

"use strict";
/* unused harmony export SubStrategy */
/* harmony export (binding) */ __webpack_require__.d(__webpack_exports__, "a", function() { return AdaptiveUberVxx; });
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_0_rxjs_add_operator_map__ = __webpack_require__(3);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_0_rxjs_add_operator_map___default = __webpack_require__.n(__WEBPACK_IMPORTED_MODULE_0_rxjs_add_operator_map__);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_1__Strategy__ = __webpack_require__(2);
var __extends = (this && this.__extends) || (function () {
    var extendStatics = Object.setPrototypeOf ||
        ({ __proto__: [] } instanceof Array && function (d, b) { d.__proto__ = b; }) ||
        function (d, b) { for (var p in b) if (b.hasOwnProperty(p)) d[p] = b[p]; };
    return function (d, b) {
        extendStatics(d, b);
        function __() { this.constructor = d; }
        d.prototype = b === null ? Object.create(b) : (__.prototype = b.prototype, new __());
    };
})();


var SubStrategy = (function () {
    function SubStrategy(p_name, p_priority, p_combination, p_tradingStartAt, p_param) {
        this.Name = "Unknown";
        this.Priority = "1"; // 0 (don't play), other: a number between 1-10.	// if Component clash at the same priority, those will be Convolved by default: added together
        this.Combination = "Avg"; //CombinationWithSamePriorities: Averaging, Adding=SimpleConvolution, InterpolationOfConvolutionAndProperIntersectionPdf
        this.StartDate = "";
        this.EndDate = "";
        this.TradingStartAt = ""; //or "20%" or "2012-01-02" (a proper date) or "100td"(trading days) "730cd" (calendar days) 2*365=730 days means 2 years, or "2y"
        this.Param = ""; // params shouldn't use &, because that is used for URI.Query parameters. Better to avoid it. Use CSV, come separation instead. Or better: ";"
        this.Name = p_name;
        this.Priority = p_priority;
        this.Combination = p_combination;
        this.TradingStartAt = p_tradingStartAt;
        this.Param = p_param;
    }
    SubStrategy.prototype.toUrlQueryString = function () {
        return "Name=" + this.Name +
            "&Priority=" + this.Priority +
            "&Combination=" + this.Combination +
            "&StartDate=" + this.StartDate +
            "&EndDate=" + this.EndDate +
            "&TradingStartAt=" + this.TradingStartAt +
            "&Param=" + this.Param;
    };
    return SubStrategy;
}());

var AdaptiveUberVxx = (function (_super) {
    __extends(AdaptiveUberVxx, _super);
    function AdaptiveUberVxx(p_app) {
        var _this = _super.call(this, "AdaptiveUberVxx", p_app) || this;
        _this.bullishTradingInstrument = ["Long SPY", "Long ^GSPC", "Long ^IXIC", "Long ^RUT", "Long QQQ", "Long QLD", "Long TQQQ", "Long IWM", "Long IYR", "Short VXXB", "Short VXXB.SQ", "Short VXZB", "Short VXZB.SQ"];
        _this.selectedBullishTradingInstrument = _this.bullishTradingInstrument[0];
        _this.param = "UseKellyLeverage=false;MaxLeverage=1.0"; // params shouldn't use &, because that is used for URI.Query parameters. Better to avoid it. Use CSV, come separation instead. Or better: ";"
        _this.fomc = new SubStrategy("FOMC", "3", "Avg", "2y", "");
        _this.holiday = new SubStrategy("Holiday", "", "", "", "");
        _this.totm = new SubStrategy("TotM", "2", "Avg", "2y", "TrainingTicker=SPY");
        _this.connor = new SubStrategy("Connor", "1", "Avg", "100td", "LookbackWindowDays=100;ProbDailyFTThreshold=47"); // LookbackDays is not all the previous history, only little window
        return _this;
    }
    AdaptiveUberVxx.prototype.IsMenuItemIdHandled = function (p_subStrategyId) {
        return p_subStrategyId == "idMenuItemAdaptiveUberVxx";
    };
    AdaptiveUberVxx.prototype.GetHtmlUiName = function (p_subStrategyId) {
        return "Learning version of UberVxx";
    };
    AdaptiveUberVxx.prototype.GetTradingViewChartName = function (p_subStrategyId) {
        return "Adaptive UberVxx";
    };
    AdaptiveUberVxx.prototype.GetWebApiName = function (p_subStrategyId) {
        return "AdaptiveUberVxx";
    };
    AdaptiveUberVxx.prototype.GetHelpUri = function (p_subStrategyId) {
        return "https://docs.google.com/document/d/1SBi8XZVB_JHsI2IIbhVpx7uDEVEXv1AVBAqPw2EkLuM";
    };
    AdaptiveUberVxx.prototype.GetStrategyParams = function (p_subStrategyId) {
        return "&BullishTradingInstrument=" + this.selectedBullishTradingInstrument + "&param=" + this.param
            + "&" + this.fomc.toUrlQueryString()
            + "&" + this.holiday.toUrlQueryString()
            + "&" + this.totm.toUrlQueryString()
            + "&" + this.connor.toUrlQueryString();
    };
    AdaptiveUberVxx.prototype.bullishTradingInstrumentChanged = function (newValue) {
        console.log("bullishTradingInstrumentChanged(): " + newValue);
        this.selectedBullishTradingInstrument = newValue;
        this.app.tipToUser = this.selectedBullishTradingInstrument;
    };
    return AdaptiveUberVxx;
}(__WEBPACK_IMPORTED_MODULE_1__Strategy__["a" /* Strategy */]));



/***/ }),
/* 21 */
/***/ (function(module, __webpack_exports__, __webpack_require__) {

"use strict";
/* harmony export (binding) */ __webpack_require__.d(__webpack_exports__, "a", function() { return AssetAllocation; });
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_0_rxjs_add_operator_map__ = __webpack_require__(3);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_0_rxjs_add_operator_map___default = __webpack_require__.n(__WEBPACK_IMPORTED_MODULE_0_rxjs_add_operator_map__);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_1__Strategy__ = __webpack_require__(2);
var __extends = (this && this.__extends) || (function () {
    var extendStatics = Object.setPrototypeOf ||
        ({ __proto__: [] } instanceof Array && function (d, b) { d.__proto__ = b; }) ||
        function (d, b) { for (var p in b) if (b.hasOwnProperty(p)) d[p] = b[p]; };
    return function (d, b) {
        extendStatics(d, b);
        function __() { this.constructor = d; }
        d.prototype = b === null ? Object.create(b) : (__.prototype = b.prototype, new __());
    };
})();


var AssetAllocation = (function (_super) {
    __extends(AssetAllocation, _super);
    function AssetAllocation(p_app) {
        var _this = _super.call(this, "AssetAllocation", p_app) || this;
        _this.assets = "MDY,ILF,FEZ,EEM,EPP,VNQ";
        _this.assetsConstantLeverage = ""; // "1,1,1,-1,1.5,2,2";
        _this.rebalancingFrequency = "Weekly,Fridays"; // "Daily,2d"(trading days),"Weekly,Fridays", "Monthly,T-1"/"Monthly,T+0" (last/first trading day of the month)
        _this.pctChannelLookbackDays = "60-120-180-252";
        _this.pctChannelPctLimits = "25-75";
        _this.isPctChannelActiveEveryDay = "Yes"; // "Yes"
        _this.isPctChannelConditional = ""; // "Yes"
        _this.histVolLookbackDays = "20";
        _this.isCashAllocatedForNonActives = ""; // "Yes"
        _this.cashEquivalentTicker = "SHY"; // "SHY"
        _this.dynamicLeverageClmtParams = ""; // "SMA(SPX,50d,200d); PR(XLU,VTI,20d)";   // SPX 50/200 crossover; PR=PriceRatio of XLU/VTI for 20 days
        _this.uberVxxEventsParams = ""; // "FOMC;Holidays"  */
        _this.debugDetailToHtml = "Date,PV,AssetFinalWeights,CashWeight,AssetData,PctChannels"; // "Date,PV,AssetFinalWeights,AssetData,PctChannels"
        _this.SetParams("idParamSetTAA_GlobVnqIbb"); // temporary for Development
        return _this;
    }
    AssetAllocation.prototype.IsMenuItemIdHandled = function (p_subStrategyId) {
        return p_subStrategyId == "idMenuItemTAA";
    };
    AssetAllocation.prototype.GetHtmlUiName = function (p_subStrategyId) {
        return "Tactical Asset Allocation (TAA)";
    };
    AssetAllocation.prototype.GetTradingViewChartName = function (p_subStrategyId) {
        return "Asset Allocation";
    };
    AssetAllocation.prototype.GetWebApiName = function (p_subStrategyId) {
        return "TAA";
    };
    AssetAllocation.prototype.GetHelpUri = function (p_subStrategyId) {
        return "https://docs.google.com/document/d/1onl2TKr-8RJqlQjsdIcN6X55fSK-4LO9k5T0Ts79-WU";
    };
    AssetAllocation.prototype.GetStrategyParams = function (p_subStrategyId) {
        return "&Assets=" + this.assets + "&AssetsConstantLeverage=" + this.assetsConstantLeverage
            + "&RebalancingFrequency=" + this.rebalancingFrequency + "&PctChannelLookbackDays=" + this.pctChannelLookbackDays
            + "&PctChannelPctLimits=" + this.pctChannelPctLimits + "&IsPctChannelActiveEveryDay=" + this.isPctChannelActiveEveryDay + "&IsPctChannelConditional=" + this.isPctChannelConditional
            + "&HistVolLookbackDays=" + this.histVolLookbackDays + "&IsCashAllocatedForNonActives=" + this.isCashAllocatedForNonActives + "&CashEquivalentTicker=" + this.cashEquivalentTicker
            + "&DynamicLeverageClmtParams=" + this.dynamicLeverageClmtParams + "&UberVxxEventsParams=" + this.uberVxxEventsParams + "&DebugDetailToHtml=" + this.debugDetailToHtml;
    };
    AssetAllocation.prototype.MenuItemParamSetsClicked = function (event) {
        console.log("MenuItemParamSetsClicked()");
        var target = event.target || event.srcElement || event.currentTarget;
        var idAttr = target.attributes.id;
        var idValue = idAttr.nodeValue;
        this.SetParams(idValue);
    };
    AssetAllocation.prototype.SetParams = function (idValue) {
        switch (idValue) {
            case "idParamSetTAA_GlobVnqIbb":
                this.assets = "MDY,ILF,FEZ,EEM,EPP,VNQ,IBB";
                this.assetsConstantLeverage = "2,2,2,2,2,2,2";
                this.rebalancingFrequency = "Daily,1d"; // Balazs: "•	Rebalance period: weekly rebalance is recommended (instead of monthly);"
                this.pctChannelLookbackDays = "50-80-120-150"; // Balazs: "•	Modified Percentile Channel Look-back Periods: 30-, 60-, 120- and 252-day is recommended (instead of 60-, 120-, 180- and 252-day);", but later decided we need Longer Term signals, so we don't switch off/on from stock too often', Balazs is using the original Varadi in all his Matlab code.
                this.pctChannelPctLimits = "20-75"; // Balazs: "•	Percentile Channel Threshold: original 25% is recommended;", but George overrides this
                this.isPctChannelActiveEveryDay = "Yes";
                this.isPctChannelConditional = "";
                this.histVolLookbackDays = "20";
                this.isCashAllocatedForNonActives = "Yes";
                this.cashEquivalentTicker = "TLT";
                this.dynamicLeverageClmtParams = "";
                this.uberVxxEventsParams = "";
                this.debugDetailToHtml = "Date,PV,AssetFinalWeights,CashWeight,AssetData,PctChannels";
                break;
            case "idParamSetTAA_Glob_Live":
                this.assets = "MDY,ILF,FEZ,EEM,EPP,VNQ";
                this.assetsConstantLeverage = "";
                this.rebalancingFrequency = "Monthly,T-1";
                this.pctChannelLookbackDays = "60-120-180-252";
                this.pctChannelPctLimits = "25-75";
                this.isPctChannelActiveEveryDay = "Yes";
                this.isPctChannelConditional = "";
                this.histVolLookbackDays = "20";
                this.isCashAllocatedForNonActives = "Yes";
                this.cashEquivalentTicker = "TLT";
                this.dynamicLeverageClmtParams = "";
                this.uberVxxEventsParams = "";
                this.debugDetailToHtml = "Date,PV,AssetFinalWeights,CashWeight,AssetData,PctChannels";
                break;
            case "idParamSetTAA_GC_Live":
                this.assets = "AAPL,AMZN,BABA,BIDU,FB,GOOGL,GWPH,NFLX,NVDA,PCLN,TSLA";
                this.assetsConstantLeverage = "";
                this.rebalancingFrequency = "Daily,1d";
                this.pctChannelLookbackDays = "60-120-180-252";
                this.pctChannelPctLimits = "25-75";
                this.isPctChannelActiveEveryDay = "Yes";
                this.isPctChannelConditional = "";
                this.histVolLookbackDays = "20";
                this.isCashAllocatedForNonActives = "Yes";
                this.cashEquivalentTicker = "";
                this.dynamicLeverageClmtParams = "";
                this.uberVxxEventsParams = "";
                this.debugDetailToHtml = "Date,PV,AssetFinalWeights,CashWeight,AssetData,PctChannels";
                break;
            case "idParamSetTAA_VaradiOriginal":
                this.assets = "VTI,ICF,LQD,DBC"; // Varadi uses SHY (1-3 Year Treasury Bond) for Cash, but Cash is fine. SHY = 0.47% CAGR only, not even half percent
                this.assetsConstantLeverage = "";
                this.rebalancingFrequency = "Monthly,T-1";
                this.pctChannelLookbackDays = "60-120-180-252";
                this.pctChannelPctLimits = "25-75";
                this.isPctChannelActiveEveryDay = "Yes";
                this.isPctChannelConditional = "";
                this.histVolLookbackDays = "20";
                this.isCashAllocatedForNonActives = "Yes";
                this.cashEquivalentTicker = "SHY";
                this.dynamicLeverageClmtParams = "";
                this.uberVxxEventsParams = "";
                this.debugDetailToHtml = "Date,PV,AssetFinalWeights,CashWeight,AssetData,PctChannels";
                break;
            default:
                //this.etf1 = "TVIX";
                //this.weight1 = "-35";      // %, negative is Short
                //this.etf2 = "TMV";
                //this.weight2 = "-65";      // %, negative is Short
                break;
        }
    }; // SetParams()
    return AssetAllocation;
}(__WEBPACK_IMPORTED_MODULE_1__Strategy__["a" /* Strategy */]));



/***/ }),
/* 22 */
/***/ (function(module, __webpack_exports__, __webpack_require__) {

"use strict";
/* harmony export (binding) */ __webpack_require__.d(__webpack_exports__, "a", function() { return LEtf; });
/* unused harmony export AngularInit_LEtf */
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_0_rxjs_add_operator_map__ = __webpack_require__(3);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_0_rxjs_add_operator_map___default = __webpack_require__.n(__WEBPACK_IMPORTED_MODULE_0_rxjs_add_operator_map__);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_1__Strategy__ = __webpack_require__(2);
var __extends = (this && this.__extends) || (function () {
    var extendStatics = Object.setPrototypeOf ||
        ({ __proto__: [] } instanceof Array && function (d, b) { d.__proto__ = b; }) ||
        function (d, b) { for (var p in b) if (b.hasOwnProperty(p)) d[p] = b[p]; };
    return function (d, b) {
        extendStatics(d, b);
        function __() { this.constructor = d; }
        d.prototype = b === null ? Object.create(b) : (__.prototype = b.prototype, new __());
    };
})();


var LEtf = (function (_super) {
    __extends(LEtf, _super);
    function LEtf(p_app) {
        var _this = _super.call(this, "LEtf", p_app) || this;
        _this.assets = "TVIX,TMV";
        _this.assetsConstantWeightPct = "-35,-65"; // "-35,-65";
        _this.rebalancingFrequency = "Daily,1d"; // "Daily,2d"(trading days),"Weekly,Fridays", "Monthly,T-1"/"Monthly,T+0" (last/first trading day of the month)
        _this.SetParams("idParamSetHL_- 35TVIX_- 65TMV");
        return _this;
    }
    LEtf.prototype.IsMenuItemIdHandled = function (p_subStrategyId) {
        return (p_subStrategyId == "idMenuItemLETFDiscrRebToNeutral") || (p_subStrategyId == "idMenuItemLETFDiscrAddToWinner") || (p_subStrategyId == "idMenuItemLETFHarryLong");
    };
    LEtf.prototype.OnStrategySelected = function (p_subStrategyId) {
        if ((p_subStrategyId == "idMenuItemLETFDiscrRebToNeutral") || (p_subStrategyId == "idMenuItemLETFDiscrAddToWinner"))
            this.SetParams("idParamSetHL_-50URE_-50SRS");
        else if (p_subStrategyId == "idMenuItemLETFHarryLong")
            this.SetParams("idParamSetHL_-50Comb.SQ_-120H_coctailAgy6");
        return true;
    };
    LEtf.prototype.GetHtmlUiName = function (p_subStrategyId) {
        switch (p_subStrategyId) {
            case "idMenuItemLETFDiscrRebToNeutral":
                return "L-ETF Discr.ToNeutral";
            case "idMenuItemLETFDiscrAddToWinner":
                return "L-ETF Discr.AddWinner";
            case "idMenuItemLETFHarryLong":
                return "Harry Long";
            default:
                return "Wrong subStrategyId!";
        }
    };
    LEtf.prototype.GetTradingViewChartName = function (p_subStrategyId) {
        return "L-ETF Discrepancy";
    };
    LEtf.prototype.GetWebApiName = function (p_subStrategyId) {
        switch (p_subStrategyId) {
            case "idMenuItemLETFDiscrRebToNeutral":
                return "LETFDiscrRebToNeutral";
            case "idMenuItemLETFDiscrAddToWinner":
                return "LETFDiscrAddToWinner";
            case "idMenuItemLETFHarryLong":
                return "LETFHarryLong";
            default:
                return "Wrong subStrategyId!";
        }
    };
    LEtf.prototype.GetHelpUri = function (p_subStrategyId) {
        switch (p_subStrategyId) {
            case "idMenuItemLETFDiscrRebToNeutral":
            case "idMenuItemLETFDiscrAddToWinner":
                return "https://docs.google.com/document/d/1IpqNT6THDP5B1C-Vugt1fA96Lf1Ms9Tb-pq0LzT3GnY";
            case "idMenuItemLETFHarryLong":
                return "https://docs.google.com/document/d/1nrWOxJNFYnLQvIxuF83ZypD_YUObiAv5nXa7Cq1x41s";
            default:
                return "Wrong subStrategyId!";
        }
    };
    LEtf.prototype.GetStrategyParams = function (p_subStrategyId) {
        return "&Assets=" + this.assets + "&AssetsConstantWeightPct=" + this.assetsConstantWeightPct
            + "&RebalancingFrequency=" + this.rebalancingFrequency;
    };
    //public etfPairsChanged(newValue) {
    //    console.log("etfPairsChanged(): " + newValue);
    //    this.selectedEtfPairs = newValue;
    //    this.app.tipToUser = this.selectedEtfPairs + "+" + this.selectedEtfPairs;
    //}
    LEtf.prototype.MenuItemParamSetsClicked = function (event) {
        console.log("MenuItemParamSetsClicked()");
        var target = event.target || event.srcElement || event.currentTarget;
        var idAttr = target.attributes.id;
        var idValue = idAttr.nodeValue;
        this.SetParams(idValue);
    };
    LEtf.prototype.SetParams = function (idValue) {
        switch (idValue) {
            case "idParamSetHL_-25TVIX_-75TMV":// original Harry Long
                this.assets = "TVIX,TMV";
                this.assetsConstantWeightPct = "-25,-75"; // %, negative is Short
                break;
            case "idParamSetHL_50VXX_50XIV":
                this.assets = "VXXB,XIV";
                this.assetsConstantWeightPct = "50,50"; // %, negative is Short
                break;
            case "idParamSetHL_-50VXX.SQ_225TLT":
                this.assets = "VXXB.SQ,TLT";
                this.assetsConstantWeightPct = "-50,225"; // %, negative is Short
                break;
            case "idParamSetHL_-50VXX_225TLT":
                this.assets = "VXXB,TLT";
                this.assetsConstantWeightPct = "-50,225"; // %, negative is Short
                break;
            case "idParamSetHL_50XIV_225TLT":
                this.assets = "XIV,TLT";
                this.assetsConstantWeightPct = "50,225"; // %, negative is Short
                break;
            case "idParamSetHL_25XIV_112TLT":// long only strategy (good) play with half leverage. would we do it? No forced Buy-in of shorts.
                this.assets = "XIV,TLT";
                this.assetsConstantWeightPct = "25,112"; // %, negative is Short
                break;
            case "idParamSetHL_-25TVIX_75CASH":
                this.assets = "TVIX,Cash";
                this.assetsConstantWeightPct = "-25,75"; // %, negative is Short
                break;
            case "idParamSetHL_25CASH_-75TMV":
                this.assets = "Cash,TMV";
                this.assetsConstantWeightPct = "25,-70"; // %, negative is Short
                break;
            // *** 50%-50% shorts ***
            case "idParamSetHL_-50URE_-50SRS":
                this.assets = "URE,SRS";
                this.assetsConstantWeightPct = "-50,-50"; // %, negative is Short
                break;
            case "idParamSetHL_-50DRN_-50DRV":
                this.assets = "DRN,DRV";
                this.assetsConstantWeightPct = "-50,-50"; // %, negative is Short
                break;
            case "idParamSetHL_-50FAS_-50FAZ":
                this.assets = "FAS,FAZ";
                this.assetsConstantWeightPct = "-50,-50"; // %, negative is Short
                break;
            case "idParamSetHL_-50VXX_-50XIV":
                this.assets = "VXXB,XIV";
                this.assetsConstantWeightPct = "-50,-50"; // %, negative is Short
                break;
            case "idParamSetHL_-50VXZ_-50ZIV":
                this.assets = "VXZB,ZIV";
                this.assetsConstantWeightPct = "-50,-50"; // %, negative is Short
                break;
            case "idParamSetHL_-35TVIX_-65TMV":
                this.assets = "TVIX,TMV";
                this.assetsConstantWeightPct = "-35,-65"; // %, negative is Short
                break;
            case "idParamSetHL_-35TVIX_-25TMV_-28UNG_-8USO_-4JJC":
                this.assets = "TVIX,TMV,UNG,USO,JJCTF";
                this.assetsConstantWeightPct = "-35,-25,-28,-8,-4"; // %, negative is Short
                break;
            case "idParamSetHL_-70VXX.SQ_-75TLT_coctailDC":
                this.assets = "VXXB.SQ,TLT,USO,UNG,JJCTF,GLD,UUP,EEM";
                this.assetsConstantWeightPct = "-70,75,-8,-13,-4,10,5,0"; // %, negative is Short
                break;
            case "idParamSetHL_-70VXX.SQ_-75TLT_coctailAgy":
                this.assets = "VXXB.SQ,TLT,USO,UNG,JJCTF,GLD,UUP,EEM";
                this.assetsConstantWeightPct = "-70,75,-8,-28,-4,0,0,0"; // %, negative is Short
                break;
            case "idParamSetHL_-70VXX.SQ_-75TLT_coctailAgy2":
                this.assets = "VXXB.SQ,TLT,USO,UNG,JJCTF,GLD,UUP,EEM";
                this.assetsConstantWeightPct = "-70,105,-10,-30,-5,0,0,0"; // %, negative is Short
                break;
            case "idParamSetHL_-70VXX.SQ_-75TLT_coctailAgy3":// Markowitz MPT optimal weight using 100% allocation
                this.assets = "VXXB.SQ,TLT,USO,UNG,JJCTF,GLD,UUP,EEM";
                this.assetsConstantWeightPct = "-70,111,-11,-34,0,0,0,0"; // %, negative is Short
                break;
            case "idParamSetHL_-70VXX.SQ_-75TLT_coctailAgy4":// Markowitz MPT optimal weight using 135% allocation
                this.assets = "VXXB.SQ,TLT,USO,UNG,JJCTF,GLD,UUP,EEM";
                this.assetsConstantWeightPct = "-70,171,-17,-52,0,0,0,0"; // %, negative is Short
                break;
            case "idParamSetHL_-70VXX.SQ_-75TLT_coctailAgy5":// Markowitz MPT optimal weight using 135% allocation
                this.assets = "VXXB.SQ,TLT,USO,UNG,CPER,GLD,UUP,EEM";
                this.assetsConstantWeightPct = "-70,213,-21,-66,0,0,0,0"; // %, negative is Short
                break;
            case "idParamSetHL_-50Comb.SQ_-80H_coctailAgy6":// shortVol is 50%, Hedge: 80%
                this.assets = "SVXY.SQ,VXXB.SQ,ZIV.SQ,TQQQ.SQ,TLT,USO,UNG,GLD,UUP,EEM";
                this.assetsConstantWeightPct = "15,-5, 10, 20, 171,-15,-54,0,0,0"; // %, negative is Short
                break;
            case "idParamSetHL_-50Comb.SQ_-100H_coctailAgy6":// shortVol is 50%, Hedge: 100%
                this.assets = "SVXY.SQ,VXXB.SQ,ZIV.SQ,TQQQ.SQ,TLT,USO,UNG,GLD,UUP,EEM";
                this.assetsConstantWeightPct = "15,-5, 10, 20, 213,-21,-66,0,0,0"; // %, negative is Short
                break;
            case "idParamSetHL_-50Comb.SQ_-120H_coctailAgy6": // shortVol is 55%, Hedge: 120%, introducing SVXY!Light0.5x.SQ
            // 2018-04: JJC went to JJCTF and went to OTC. Because it is OTC, IB doesn't give realtime price, so chart will go until yesterday only. CPER has realtime price, but its history from 2011, instead of 2007. As JJCTF weight is 0, delete it from here. Backtest will be faster anyway.
            default:
                this.assets = "SVXY!Light0.5x.SQ,VXXB.SQ,ZIV.SQ,TQQQ.SQ,TLT,USO,UNG,GLD,UUP,EEM";
                this.assetsConstantWeightPct = "15,-5, 10, 25, 255,-27,-78,0,0,0"; // %, negative is Short
                break;
        }
    };
    return LEtf;
}(__WEBPACK_IMPORTED_MODULE_1__Strategy__["a" /* Strategy */]));

function AngularInit_LEtf(app) {
    console.log("AngularInit_LEtf() START, AppComponent.version: " + app.m_versionShortInfo);
    //app.etfPairs = ["URE-SRS", "DRN-DRV", "FAS-FAZ", "XIV-VXX", "ZIV-VXZB",];
    //app.selectedEtfPairs = "URE-SRS";
    ////app.selectedEtfPairsIdx = 1;   // zero based, so it is December
    //app.rebalancingFrequency = "5d";
}


/***/ }),
/* 23 */
/***/ (function(module, __webpack_exports__, __webpack_require__) {

"use strict";
/* harmony export (binding) */ __webpack_require__.d(__webpack_exports__, "a", function() { return TotM; });
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_0_rxjs_add_operator_map__ = __webpack_require__(3);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_0_rxjs_add_operator_map___default = __webpack_require__.n(__WEBPACK_IMPORTED_MODULE_0_rxjs_add_operator_map__);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_1__Strategy__ = __webpack_require__(2);
var __extends = (this && this.__extends) || (function () {
    var extendStatics = Object.setPrototypeOf ||
        ({ __proto__: [] } instanceof Array && function (d, b) { d.__proto__ = b; }) ||
        function (d, b) { for (var p in b) if (b.hasOwnProperty(p)) d[p] = b[p]; };
    return function (d, b) {
        extendStatics(d, b);
        function __() { this.constructor = d; }
        d.prototype = b === null ? Object.create(b) : (__.prototype = b.prototype, new __());
    };
})();


var TotM = (function (_super) {
    __extends(TotM, _super);
    function TotM(p_app) {
        var _this = _super.call(this, "TotM", p_app) || this;
        _this.bullishTradingInstrument = ["Long SPY", "Long ^GSPC", "Long ^IXIC", "Long ^RUT", "Long QQQ", "Long QLD", "Long TQQQ", "Long IWM", "Long IYR", "Short VXXB", "Short VXXB.SQ", "Short VXZB", "Short VXZB.SQ"];
        _this.selectedBullishTradingInstrument = _this.bullishTradingInstrument[0];
        //public totMStock = ["SPY", "QQQ", "VXXB"];
        //public selectedTotMStock = "SPY";
        //public totMLongOrShortWhenBullish = ["Long", "Short"];
        //public selectedTotMLongOrShortWhenBullish = "Long";
        //public dailyMarketDirectionMaskSummerTotM = "D.UUU00DD";  //Mask: D0.UU, Up: Market Up, D: Down, 0:Cash (B is not good because Bullish, Bearish): other option Comma separation, but not necessary here
        //public dailyMarketDirectionMaskSummerTotMM = "DDUU.UU";
        //public dailyMarketDirectionMaskWinterTotM = "D.UUU00DD";
        //public dailyMarketDirectionMaskWinterTotMM = "DDUU.UU";
        // before significance test: SPY: CAGR:  25.30%  Annualized StDev:  16.50%  Sharpe:  1.53; (15+19)/2=17 days per month
        //public dailyMarketDirectionMaskWinterTotM = "UUUD.UUU";//Mask: D0.UU, Up: Market Up, D: Down, 0:Cash (B is not good because Bullish, Bearish): other option Comma separation, but not necessary here
        //public dailyMarketDirectionMaskWinterTotMM = "DDUU.UU00UU";
        //public dailyMarketDirectionMaskSummerTotM = "DDDDUUD.UDD";
        //public dailyMarketDirectionMaskSummerTotMM = "DDUU.UU00DDD";
        // after significance test: SPY: CAGR:  23.27%  Annualized StDev:  14.23%  Sharpe:  1.64; (15+8)/2=11.5 days per month //sharpe increased! more reliable 
        _this.dailyMarketDirectionMaskWinterTotM = "UUUD.UUU"; //Mask: D0.UU, Up: Market Up, D: Down, 0:Cash (B is not good because Bullish, Bearish): other option Comma separation, but not necessary here
        _this.dailyMarketDirectionMaskWinterTotMM = "DDUU.UU00UU"; // winter didn't change after Significance test.
        _this.dailyMarketDirectionMaskSummerTotM = "DD00U00.U";
        _this.dailyMarketDirectionMaskSummerTotMM = "D0UU.0U";
        return _this;
    }
    TotM.prototype.IsMenuItemIdHandled = function (p_subStrategyId) {
        return p_subStrategyId == "idMenuItemTotM";
    };
    TotM.prototype.GetHtmlUiName = function (p_subStrategyId) {
        return "Turn of the Month (mask based). Typical: Bearish:T-1, Bullish: T+1,T+2,T+3";
    };
    TotM.prototype.GetTradingViewChartName = function (p_subStrategyId) {
        return "Turn of the Month";
    };
    TotM.prototype.GetWebApiName = function (p_subStrategyId) {
        return "TotM";
    };
    TotM.prototype.GetHelpUri = function (p_subStrategyId) {
        return "https://docs.google.com/document/d/1DJtSt1FIPFbscAZsn8UAfiBBIhbeWvZcJWtQffGPTfU";
    };
    TotM.prototype.GetStrategyParams = function (p_subStrategyId) {
        return "&BullishTradingInstrument=" + this.selectedBullishTradingInstrument
            + "&DailyMarketDirectionMaskSummerTotM=" + this.dailyMarketDirectionMaskSummerTotM + "&DailyMarketDirectionMaskSummerTotMM=" + this.dailyMarketDirectionMaskSummerTotMM
            + "&DailyMarketDirectionMaskWinterTotM=" + this.dailyMarketDirectionMaskWinterTotM + "&DailyMarketDirectionMaskWinterTotMM=" + this.dailyMarketDirectionMaskWinterTotMM;
    };
    TotM.prototype.bullishTradingInstrumentChanged = function (newValue) {
        console.log("bullishTradingInstrumentChanged(): " + newValue);
        this.selectedBullishTradingInstrument = newValue;
        this.app.tipToUser = this.selectedBullishTradingInstrument;
    };
    TotM.prototype.MenuItemPresetMasksClicked = function (event) {
        console.log("MenuItemPresetMasksClicked()");
        var target = event.target || event.srcElement || event.currentTarget;
        var idAttr = target.attributes.id;
        var idValue = idAttr.nodeValue;
        switch (idValue) {
            case "idMaskBuyHold":
                this.dailyMarketDirectionMaskWinterTotM = "UUUUUUUUUUUUUUUUUUUU.UUUUUUUUUUUUUUUUUUUU"; // 20 days before and 20 days after Turn of the Month is set (to be sure)
                this.dailyMarketDirectionMaskWinterTotMM = "UUUUUUUUUUUUUUUUUUUU.UUUUUUUUUUUUUUUUUUUU";
                this.dailyMarketDirectionMaskSummerTotM = "UUUUUUUUUUUUUUUUUUUU.UUUUUUUUUUUUUUUUUUUU";
                this.dailyMarketDirectionMaskSummerTotMM = "UUUUUUUUUUUUUUUUUUUU.UUUUUUUUUUUUUUUUUUUU";
                break;
            case "idMaskUberVXXOld":
                // TotM:
                //•	Long VXX on Day -1 (last trading day of the month) with 100%;
                //•	Short VXX on Day 1-3 (first three trading days of the month) with 100%.
                this.dailyMarketDirectionMaskWinterTotM = "D.UUU";
                this.dailyMarketDirectionMaskWinterTotMM = ".";
                this.dailyMarketDirectionMaskSummerTotM = "D.UUU";
                this.dailyMarketDirectionMaskSummerTotMM = ".";
                break;
            case "idMaskBuyUberVXXNew":// Correlation and Significance Analysis of Uber VXX Strategy Parts.docx
                // TotM:
                //•	Day -1: long VXX - both in winter and summer;
                //•	Day +1: short VXX only at turn of the quarter - both in winter and summer;
                //•	Day +2-+3: short VXX only in winter.
                // TotMM: 
                //•	Day +2: short VXX - both in winter and summer;
                //•	Day +3-+7: short VXX only in winter.
                this.dailyMarketDirectionMaskWinterTotM = "D.UUU"; // "• Day +1: short VXX only at turn of the quarter - both in winter and summer;", but I put it as Bullish anyway
                this.dailyMarketDirectionMaskWinterTotMM = ".0UUUUUU";
                this.dailyMarketDirectionMaskSummerTotM = "D.U"; // "• Day +1: short VXX only at turn of the quarter - both in winter and summer;", but I put it as Bullish anyway
                this.dailyMarketDirectionMaskSummerTotMM = ".0U";
                break;
            default://SPYDerived
                this.dailyMarketDirectionMaskWinterTotM = "UUUD.UUU"; //Mask: D0.UU, Up: Market Up, D: Down, 0:Cash (B is not good because Bullish, Bearish): other option Comma separation, but not necessary here
                this.dailyMarketDirectionMaskWinterTotMM = "DDUU.UU00UU"; // winter didn't change after Significance test.
                this.dailyMarketDirectionMaskSummerTotM = "DD00U00.U";
                this.dailyMarketDirectionMaskSummerTotMM = "D0UU.0U";
        }
    };
    //public InvertVisibilityOfTableRow(event) {
    //    console.log("InvertVisibilityOfTableRow() START)");
    //    var target = event.target || event.srcElement || event.currentTarget;
    //    var idAttr = target.attributes.id;
    //    var idValue = idAttr.nodeValue;
    //    var tableRow = document.getElementById(idValue);
    //    if (tableRow.style.display == 'none')
    //        tableRow.style.display = 'table-row';
    //    else
    //        tableRow.style.display = 'none';
    //}
    // Angular2 (click) event didn't call these. We have to force somehow Angular2 to reparse the DOM for (onclicks) after we inserted the extra table. 
    // I found a workaround now in global JS, not in TS.Angular2 is still not final.Fix this after Angular2 is final.
    TotM.prototype.InvertVisibilityOfTableRow = function (paramID) {
        console.log("InvertVisibilityOfTableRow() START");
        var tableRow = document.getElementById(paramID);
        if (tableRow != null)
            if (tableRow.style.display == 'none')
                tableRow.style.display = 'table-row';
            else
                tableRow.style.display = 'none';
    };
    return TotM;
}(__WEBPACK_IMPORTED_MODULE_1__Strategy__["a" /* Strategy */]));

var GlobalScopeInvertVisibilityOfTableRow_SomehowItDidntWorkYet = function (paramID) {
    console.log("GlobalScopeInvertVisibilityOfTableRow() START");
    var tableRow = document.getElementById(paramID);
    if (tableRow != null)
        if (tableRow.style.display == 'none')
            tableRow.style.display = 'table-row';
        else
            tableRow.style.display = 'none';
};


/***/ }),
/* 24 */
/***/ (function(module, __webpack_exports__, __webpack_require__) {

"use strict";
/* harmony export (binding) */ __webpack_require__.d(__webpack_exports__, "a", function() { return VXX_SPY_Controversial; });
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_0_rxjs_add_operator_map__ = __webpack_require__(3);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_0_rxjs_add_operator_map___default = __webpack_require__.n(__WEBPACK_IMPORTED_MODULE_0_rxjs_add_operator_map__);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_1__Strategy__ = __webpack_require__(2);
var __extends = (this && this.__extends) || (function () {
    var extendStatics = Object.setPrototypeOf ||
        ({ __proto__: [] } instanceof Array && function (d, b) { d.__proto__ = b; }) ||
        function (d, b) { for (var p in b) if (b.hasOwnProperty(p)) d[p] = b[p]; };
    return function (d, b) {
        extendStatics(d, b);
        function __() { this.constructor = d; }
        d.prototype = b === null ? Object.create(b) : (__.prototype = b.prototype, new __());
    };
})();


var VXX_SPY_Controversial = (function (_super) {
    __extends(VXX_SPY_Controversial, _super);
    // Balazs's parameter was 0.1% and 0.125%, but that decreased the profit
    // with spyMinPctMove == 0.01, vxxMinPctMove = 0.01, go To Cash: I got better CAGR than the Going Short (Going Long is bad, because of volatility drag)
    // increasing vxxMinPctMove is not good, because when vxxPctMove is very, very high, next day can be strong MR, so VXX can go down a lot. We don't want to miss those profits, so we don't increase the vxxMinPctMove too much
    //this.app.selectedEtfPairsIdx = 1;   // zero based, so it is December
    function VXX_SPY_Controversial(p_app) {
        var _this = _super.call(this, "VXX_SPY_Controversial", p_app) || this;
        _this.vxxLongOrShortTrade = ["Long", "Short", "Cash"];
        _this.selectedVXXLongOrShortTrade = "Cash";
        _this.spyMinPctMove = "0.01";
        _this.vxxMinPctMove = "0.01"; // corresponding to 0.25% of VIX move, with VXX Beta = 2 approximately
        return _this;
    }
    VXX_SPY_Controversial.prototype.IsMenuItemIdHandled = function (p_subStrategyId) {
        return p_subStrategyId == "idMenuItemVXX_SPY_Controversial";
    };
    VXX_SPY_Controversial.prototype.GetHtmlUiName = function (p_subStrategyId) {
        return "Buy&Hold XIV with VXX-SPY ControversialDay: Cash if VXX & SPY move in the same direction";
    };
    VXX_SPY_Controversial.prototype.GetTradingViewChartName = function (p_subStrategyId) {
        return "VXX-SPY ControversialDay";
    };
    VXX_SPY_Controversial.prototype.GetWebApiName = function (p_subStrategyId) {
        return "VXX_SPY_Controversial";
    };
    VXX_SPY_Controversial.prototype.GetHelpUri = function (p_subStrategyId) {
        return "https://docs.google.com/document/d/1G1gjvt9GdqB4yrAvLV4ELnVDYNd587tovcWrVzTwqak";
    };
    VXX_SPY_Controversial.prototype.GetStrategyParams = function (p_subStrategyId) {
        return "&SpyMinPctMove=" + this.spyMinPctMove + "&VxxMinPctMove=" + this.vxxMinPctMove + "&LongOrShortTrade=" + this.selectedVXXLongOrShortTrade;
    };
    VXX_SPY_Controversial.prototype.vxxLongOrShortTradeChanged = function (newValue) {
        console.log("vxxLongOrShortTradeChanged(): " + newValue);
        this.selectedVXXLongOrShortTrade = newValue;
        this.app.tipToUser = this.selectedVXXLongOrShortTrade + "+" + this.selectedVXXLongOrShortTrade;
    };
    return VXX_SPY_Controversial;
}(__WEBPACK_IMPORTED_MODULE_1__Strategy__["a" /* Strategy */]));



/***/ }),
/* 25 */
/***/ (function(module, __webpack_exports__, __webpack_require__) {

"use strict";
/* harmony export (binding) */ __webpack_require__.d(__webpack_exports__, "a", function() { return QuickTesterComponent; });
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_0__angular_core__ = __webpack_require__(1);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_1__angular_http__ = __webpack_require__(5);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_2__Strategies_VXX_SPY_Controversial__ = __webpack_require__(24);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_3__Strategies_LEtf__ = __webpack_require__(22);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_4__Strategies_TotM__ = __webpack_require__(23);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_5__Strategies_AdaptiveUberVxx__ = __webpack_require__(20);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_6__Strategies_AssetAllocation__ = __webpack_require__(21);
var __decorate = (this && this.__decorate) || function (decorators, target, key, desc) {
    var c = arguments.length, r = c < 3 ? target : desc === null ? desc = Object.getOwnPropertyDescriptor(target, key) : desc, d;
    if (typeof Reflect === "object" && typeof Reflect.decorate === "function") r = Reflect.decorate(decorators, target, key, desc);
    else for (var i = decorators.length - 1; i >= 0; i--) if (d = decorators[i]) r = (c < 3 ? d(r) : c > 3 ? d(target, key, r) : d(target, key)) || r;
    return c > 3 && r && Object.defineProperty(target, key, r), r;
};
var __metadata = (this && this.__metadata) || function (k, v) {
    if (typeof Reflect === "object" && typeof Reflect.metadata === "function") return Reflect.metadata(k, v);
};







var DailyBar = (function () {
    function DailyBar() {
    }
    return DailyBar;
}());
var QuickTesterComponent = (function () {
    function QuickTesterComponent(http) {
        this.http = http;
        this.m_userEmail = 'Unknown user';
        this.m_versionShortInfo = "v0.2.32"; // strongly typed variables in TS
        this.versionLongInfo = "SQ QuickTester  \nVersion 0.2.32  \nDeployed: 2016-11-04T21:00Z"; // Z means Zero UTC offset, so, it is the UTC time, http://en.wikipedia.org/wiki/ISO_8601
        this.tipToUser = "Select Strategy and press 'Start Backtest'...";
        this.tradingViewChartWidget = null;
        this.tradingViewChartName = "DayOfTheWeek data";
        this.inputStartDateStr = ""; // empty string means maximum available
        this.inputEndDateStr = ""; // empty string means: today
        this.selectedSubStrategyMenuItemId = ""; // This identifies the substrategy under Strategy.  Also, the HTML hidden or visible parts are controlled by this. 
        this.selectedSubStrategyName = "";
        this.selectedSubStrategyHelpUri = "";
        this.profilingBacktestStopWatch = null;
        this.profilingBacktestCallbackMSec = null;
        this.profilingBacktestAtChartReadyStartMSec = null;
        this.profilingBacktestAtChartReadyEndMSec = null;
        // Output Statistics area
        this.startDateStr = "";
        this.endDateStr = "";
        this.rebalanceFrequencyStr = "";
        this.benchmarkStr = "";
        this.pvStartValue = 1;
        this.pvEndValue = 1;
        this.totalGainPct = 1;
        this.cagr = 1;
        this.annualizedStDev = 1;
        this.sharpeRatio = 1;
        this.sortinoRatio = 1;
        this.maxDD = 1;
        this.ulcerInd = 1; // = qMean DD
        this.maxTradingDaysInDD = 1;
        this.winnersStr = 1;
        this.losersStr = 1;
        this.benchmarkCagr = 1;
        this.benchmarkMaxDD = 1;
        this.benchmarkCorrelation = 0;
        this.pvCash = 1;
        this.nPositions = 0;
        this.holdingsListStr = "";
        this.htmlNoteFromStrategy = "";
        this.chartDataFromServer = null; // original, how it arrived from server
        this.chartDataInStr = null; // for showing it in HTML for debug porposes
        //public chartDataToChart = null; 
        this.chartDataToChart = []; //// processed: it has time: close, open values, so we have to process it only once
        this.nMonthsInTimeFrame = "24";
        this.startDateUtc = new Date(2000, 0, 1, 1, 0); // 1st January, 2000, T: 1:00
        this.endDateUtc = new Date(2000, 0, 1, 1, 0); // 1st January, 2000, T: 1:00
        this.debugMessage = "";
        this.errorMessage = "";
        this.clickMessage = '';
    }
    QuickTesterComponent.prototype.ngOnInit = function () {
        console.log("ngOnInit() START");
        //this.getHMData(gDefaultHMData);
        if (typeof gSqUserEmail == "undefined")
            this.m_userEmail = 'undefined@gmail.com';
        else
            this.m_userEmail = gSqUserEmail;
        this.strategy_LEtf = new __WEBPACK_IMPORTED_MODULE_3__Strategies_LEtf__["a" /* LEtf */](this);
        this.strategy_VXX_SPY_Controversial = new __WEBPACK_IMPORTED_MODULE_2__Strategies_VXX_SPY_Controversial__["a" /* VXX_SPY_Controversial */](this);
        this.strategy_TotM = new __WEBPACK_IMPORTED_MODULE_4__Strategies_TotM__["a" /* TotM */](this);
        this.strategy_AdaptiveUberVxx = new __WEBPACK_IMPORTED_MODULE_5__Strategies_AdaptiveUberVxx__["a" /* AdaptiveUberVxx */](this);
        this.strategy_AssetAllocation = new __WEBPACK_IMPORTED_MODULE_6__Strategies_AssetAllocation__["a" /* AssetAllocation */](this);
        this.strategies = [this.strategy_LEtf, this.strategy_VXX_SPY_Controversial, this.strategy_TotM, this.strategy_AdaptiveUberVxx, this.strategy_AssetAllocation];
        //this.SelectStrategy("idMenuItemAdaptiveUberVxx"); // there is no #if DEBUG in TS yet. We use TotM rarely in production anyway, so UberVXX can be the default, even while developing it.
        this.SelectStrategy("idMenuItemTAA"); // temporary default until it is being developed
        this.TradingViewChartOnready();
    };
    QuickTesterComponent.prototype.ngAfterViewInit = function () {
        //Ideally you should wait till component content get initialized, in order to make the DOM available on which you wanted to apply jQuery. For that you need to use AfterViewInit which is one of hook of angular2 lifecycle.
        //here you will have code where component content is ready.
        console.log("ngAfterViewInit() START");
        if (typeof $ == "undefined") {
            console.log("ngAfterViewInit() called. $ is undefined");
            return;
        }
        console.log("ngAfterViewInit() START 2");
    };
    QuickTesterComponent.prototype.SelectStrategy = function (menuItemId) {
        this.selectedSubStrategyMenuItemId = menuItemId;
        for (var i = 0; i < this.strategies.length; i++) {
            var strategy = this.strategies[i];
            if (strategy.IsMenuItemIdHandled(menuItemId)) {
                this.selectedStrategy = strategy;
                strategy.OnStrategySelected(menuItemId);
                this.selectedSubStrategyName = strategy.GetHtmlUiName(menuItemId);
                this.selectedSubStrategyHelpUri = strategy.GetHelpUri(menuItemId);
                break;
            }
        }
    };
    QuickTesterComponent.prototype.TradingViewChartOnready = function () {
        // on server side rendering <HEAD> of HTML is not processed, so gTradingViewChartOnreadyCalled and even charting_library.min.js is not processed yet.
        if (typeof gTradingViewChartOnreadyCalled == "undefined")
            console.log("TradingViewChartOnready() called by ngOnInit(). Global gTradingViewOnreadyCalled: " + "UNDEFINED");
        else
            console.log("TradingViewChartOnready() called by ngOnInit(). Global gTradingViewOnreadyCalled: " + gTradingViewChartOnreadyCalled);
        if (typeof TradingView == "undefined") {
            console.log("TradingViewChartOnready() called by ngOnInit(). TradingView is undefined");
            return;
        }
        //https://github.com/tradingview/charting_library/wiki/Widget-Constructor
        var widget = new TradingView.widget({
            //fullscreen: true,
            symbol: 'PV',
            //symbol: 'AA',
            interval: 'D',
            container_id: "tv_chart_container",
            //	BEWARE: no trailing slash is expected in feed URL
            datafeed: new Datafeeds.UDFCompatibleDatafeed(this, "http://demo_feed.tradingview.com"),
            library_path: "/charting_library/charting_library/",
            locale: getParameterByName('lang') || "en",
            drawings_access: { type: 'black', tools: [{ name: "Regression Trend" }] },
            charts_storage_url: 'http://saveload.tradingview.com',
            charts_storage_api_version: "1.1",
            client_id: 'tradingview.com',
            user_id: 'public_user_id',
            width: "90%" //Remark: if you want the chart to occupy all the available space, do not use '100%' in those field. Use fullscreen parameter instead (see below). It's because of issues with DOM nodes resizing in different browsers.
            ,
            height: 400
            //https://github.com/tradingview/charting_library/wiki/Featuresets
            //,enabled_features: ["trading_options"]    
            //, enabled_features: ["charting_library_debug_mode", "narrow_chart_enabled", "move_logo_to_main_pane"] //narrow_chart_enabled and move_logo_to_main_pane doesn't do anything to me
            //, enabled_features: ["charting_library_debug_mode"]
            //, disabled_features: ["use_localstorage_for_settings", "volume_force_overlay", "left_toolbar", "control_bar", "timeframes_toolbar", "border_around_the_chart", "header_widget"]
            ,
            disabled_features: ["border_around_the_chart"],
            debug: true // Setting this property to true makes the chart to write detailed API logs to console. Feature charting_library_debug_mode is a synonym for this field usage.
            ,
            time_frames: [
                { text: this.nMonthsInTimeFrame + "m", resolution: "D" },
                { text: this.nMonthsInTimeFrame + "m", resolution: "W" },
                { text: this.nMonthsInTimeFrame + "m", resolution: "M" },
            ],
            overrides: {
                "mainSeriesProperties.style": 3,
                "symbolWatermarkProperties.color": "#644",
                "moving average exponential.length": 13 // but doesn't work. It will be changed later anyway.
            },
        });
        this.tradingViewChartWidget = widget;
        var that = this;
        widget.onChartReady(function () {
            console.log("widget.onChartReady()");
            var chartWidget = that.tradingViewChartWidget;
            chartWidget.chart().createStudy('Moving Average Exponential', false, false, [26]); //inputs: (since version 1.2) an array of study inputs.
            // Decision: don't use setVisibleRange(), because even if we set up EndDate as 5 days in the future, it cuts the chart until 'today' sharp.
            // leave the chart as default, that gives about 5 empty days in the future, which we want. Which looks nice.
            //if (that.endDateUtc != null) {
            //    var visibleRangeStartDateUtc = new Date();
            //    visibleRangeStartDateUtc.setTime(that.endDateUtc.getTime() - 365 * 24 * 1000 * 60 * 60);       // assuming 365 calendar days per year, set the visible range for the last 1 year
            //    visibleRangeStartDateUtc.setHours(0, 0, 0, 0);
            //    var visibleRangeEndDateUtc = new Date();
            //    visibleRangeEndDateUtc.setTime(that.endDateUtc.getTime() + 5 * 24 * 1000 * 60 * 60);       // it is nice (and by default) the visible endDate is about 5 days in the future (after today)
            //    visibleRangeEndDateUtc.setHours(0, 0, 0, 0);
            //    if (visibleRangeStartDateUtc < visibleRangeEndDateUtc) {
            //        console.log("widget.onChartReady(): chart().setVisibleRange()");
            //        var oldVisibleRange = that.tradingViewChartWidget.chart().getVisibleRange();       
            //        // getVisibleRange gives back Wrong time range: {"from":1459814400,"to":0} but 
            //        // setVisibleRange doesn't accept that: gives back to Console: "Wrong time range: {"from":1459814400,"to":0} ". So it is buggy.
            //        that.tradingViewChartWidget.chart().setVisibleRange({       // this was introduced per my request: https://github.com/tradingview/charting_library/issues/320
            //            from: visibleRangeStartDateUtc.getTime() / 1000,
            //            to: oldVisibleRange.to                                      // if we give '0', it says: "Wrong time range:
            //            //to: visibleRangeEndDateUtc.getTime() / 1000               // if we give 'today + 5 days' in the future, it still cuts the chart at today. Bad.
            //        });
            //    }
            //}
        });
    }; // TradingViewChartOnready()
    QuickTesterComponent.prototype.MenuItemStartBacktestClicked = function () {
        console.log("MenuItemStartBacktestClicked() START");
        var generalInputParameters = "StartDate=" + this.inputStartDateStr + "&EndDate=" + this.inputEndDateStr;
        this.selectedStrategy.StartBacktest(this.http, generalInputParameters, this.selectedSubStrategyMenuItemId);
        //this.profilingBacktestStopWatch = new StopWatch();
        //this.profilingBacktestStopWatch.Start();
    };
    QuickTesterComponent.prototype.MenuItemVersionInfoClicked = function () {
        alert(this.versionLongInfo);
    };
    QuickTesterComponent.prototype.ProcessStrategyResult = function (strategyResult) {
        console.log("ProcessStrategyResult() START");
        //this.profilingBacktestCallbackMSec = this.profilingBacktestStopWatch.GetTimestampInMsec();
        if (strategyResult.errorMessage != "") {
            alert(strategyResult.errorMessage);
            return; // in this case, don't do anything; there is no real Data.
        }
        this.startDateStr = strategyResult.startDateStr;
        this.rebalanceFrequencyStr = strategyResult.rebalanceFrequencyStr;
        this.benchmarkStr = strategyResult.benchmarkStr;
        this.endDateStr = strategyResult.endDateStr;
        this.pvStartValue = strategyResult.pvStartValue;
        this.pvEndValue = strategyResult.pvEndValue;
        this.totalGainPct = strategyResult.totalGainPct;
        this.cagr = strategyResult.cagr;
        this.annualizedStDev = strategyResult.annualizedStDev;
        this.sharpeRatio = strategyResult.sharpeRatio;
        this.sortinoRatio = strategyResult.sortinoRatio;
        this.maxDD = strategyResult.maxDD;
        this.ulcerInd = strategyResult.ulcerInd;
        this.maxTradingDaysInDD = strategyResult.maxTradingDaysInDD;
        this.winnersStr = strategyResult.winnersStr;
        this.losersStr = strategyResult.losersStr;
        this.benchmarkCagr = strategyResult.benchmarkCagr;
        this.benchmarkMaxDD = strategyResult.benchmarkMaxDD;
        this.benchmarkCorrelation = strategyResult.benchmarkCorrelation;
        this.pvCash = strategyResult.pvCash;
        this.nPositions = strategyResult.nPositions;
        this.holdingsListStr = strategyResult.holdingsListStr;
        this.htmlNoteFromStrategy = strategyResult.htmlNoteFromStrategy;
        var htmlElementNote = document.getElementById("idHtmlNoteFromStrategy");
        if (htmlElementNote != null)
            htmlElementNote.innerHTML = strategyResult.htmlNoteFromStrategy;
        this.debugMessage = strategyResult.debugMessage;
        this.errorMessage = strategyResult.errorMessage;
        this.chartDataFromServer = strategyResult.chartData;
        this.chartDataToChart = [];
        var prevDayClose = -1;
        for (var i = 0; i < strategyResult.chartData.length; i++) {
            var rowParts = strategyResult.chartData[i].split(",");
            var dateParts = rowParts[0].split("-");
            var dateUtc = new Date(Date.UTC(parseInt(dateParts[0]), parseInt(dateParts[1]) - 1, parseInt(dateParts[2]), 0, 0, 0));
            var closePrice = parseFloat(rowParts[1]);
            var openPrice = (i == 0) ? closePrice : prevDayClose;
            var barValue = {
                time: dateUtc.getTime(),
                close: closePrice,
                open: openPrice,
                high: (i == 0) ? closePrice : ((openPrice > closePrice) ? openPrice : closePrice),
                low: (i == 0) ? closePrice : ((openPrice < closePrice) ? openPrice : closePrice)
            };
            prevDayClose = barValue.close;
            this.chartDataToChart.push(barValue);
        }
        // calculate number of months in the range
        this.startDateUtc = new Date(this.chartDataToChart[0].time);
        this.endDateUtc = new Date(this.chartDataToChart[this.chartDataToChart.length - 1].time);
        var nMonths = (this.endDateUtc.getFullYear() - this.startDateUtc.getFullYear()) * 12;
        nMonths -= this.startDateUtc.getMonth() + 1;
        nMonths += this.endDateUtc.getMonth();
        nMonths = nMonths <= 0 ? 1 : nMonths; // if month is less than 0, tell the chart to have 1 month
        this.chartDataInStr = strategyResult.chartData.reverse().join("\n");
        this.nMonthsInTimeFrame = nMonths.toString();
        //////***!!!!This is the best if we have to work with the official Chart, but postMessage works without this
        //////  Refresh TVChart (make it call the getBars()), version 2: idea stolen from widget.setLangue() inner implementation. It will redraw the Toolbars too, not only the inner area. But it can change TimeFrames Toolbar
        // this part will set up the Timeframes bar properly, but later is chart.onChartReady() you have to click the first button by "dateRangeDiv.children['0'].click();"
        if ((this.tradingViewChartWidget != null) && (this.tradingViewChartWidget._ready != null) && (this.tradingViewChartWidget._ready == true)) {
            // we have 2 options: 1. or 2. to refresh chart after data arrived:
            // 1. update chart without recreating the whole chartWidget. This would be smooth and not blink.
            // setVisibleRange() nicely works, by our request, but the time_frames[] are not updated. So, it is not ideal. So, choose to remove and recreate the chart instead.
            //this.tradingViewChartWidget.options.time_frames[0].text = nMonths + "m";
            //this.tradingViewChartWidget.options.time_frames[1].text = nMonths + "m";
            //this.tradingViewChartWidget.options.time_frames[2].text = nMonths + "m";
            //this.tradingViewChartWidget.removeAllStudies();
            //this.tradingViewChartWidget.setSymbol("PV", 'D');
            //this.tradingViewChartWidget.chart().setVisibleRange({       // this was introduced per my request: https://github.com/tradingview/charting_library/issues/320
            //    from: Date.UTC(2012, 2, 3) / 1000,
            //    to: Date.UTC(2015, 3, 3) / 1000
            //});
            //this.tradingViewChartWidget.createStudy('Moving Average Exponential', false, false, [26]);
            // 2. Update the chart with recreating the whole frame. This will blink, as the frame part will disappear. However, it is quite quick, so it is ok.
            this.tradingViewChartWidget.remove(); // this is the way to the widget.options to be effective
            ////gTradingViewChartWidget.options.time_frames[0].text = "All";    // cannot be "All"; it crashes.
            this.tradingViewChartWidget.options.time_frames[0].text = nMonths + "m";
            this.tradingViewChartWidget.options.time_frames[1].text = nMonths + "m";
            this.tradingViewChartWidget.options.time_frames[2].text = nMonths + "m";
            ////gTradingViewChartWidget.options.width = "50%";        // works too in Remove(), Create()
            this.tradingViewChartWidget.create();
        }
        console.log("ProcessStrategyResult() END");
    };
    QuickTesterComponent.prototype.onHeadProcessing = function () {
        console.log('onHeadProcessing()');
    };
    QuickTesterComponent.prototype.MenuItemStrategyClick = function (event) {
        console.log("MenuItemStrategyClick() START");
        $(".sqMenuBarLevel2").hide();
        $(".sqMenuBarLevel1").hide();
        var target = event.target || event.srcElement || event.currentTarget;
        var idAttr = target.attributes.id;
        var value = idAttr.nodeValue;
        this.SelectStrategy(value);
    };
    QuickTesterComponent.prototype.SQToggle = function (hiddenTextID, alwaysVisibleSwitchID, switchDisplayText) {
        console.log("SQToggle() START");
        var hiddenText = document.getElementById(hiddenTextID);
        var switchElement = document.getElementById(alwaysVisibleSwitchID);
        if (hiddenText != null && switchElement != null)
            if (hiddenText.style.display == "block") {
                hiddenText.style.display = "none";
                switchElement.innerHTML = "+ Show " + switchDisplayText;
            }
            else {
                hiddenText.style.display = "block";
                switchElement.innerHTML = "- Hide " + switchDisplayText;
            }
    };
    QuickTesterComponent.prototype.OnParameterInputKeypress = function (event) {
        var chCode = ('charCode' in event) ? event.charCode : event.keyCode;
        if (chCode == 13)
            this.MenuItemStartBacktestClicked();
        //alert("The Unicode character code is: " + chCode);
    };
    QuickTesterComponent.prototype.onClickMe = function () {
        this.clickMessage = 'You are my hero!';
    };
    QuickTesterComponent = __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Component"])({
            selector: 'quicktester',
            template: __webpack_require__(39),
            styles: [__webpack_require__(48)]
        }),
        __metadata("design:paramtypes", [__WEBPACK_IMPORTED_MODULE_1__angular_http__["Http"]])
    ], QuickTesterComponent);
    return QuickTesterComponent;
}());

// ************** Utils section with Global functions
function getParameterByName(name) {
    name = name.replace(/[\[]/, "\\[").replace(/[\]]/, "\\]");
    var regex = new RegExp("[\\?&]" + name + "=([^&#]*)"), results = regex.exec(location.search);
    return results === null ? "" : decodeURIComponent(results[1].replace(/\+/g, " "));
}


/***/ }),
/* 26 */
/***/ (function(module, exports, __webpack_require__) {

exports = module.exports = __webpack_require__(4)(undefined);
// imports


// module
exports.push([module.i, "@media (max-width: 767px) {\r\n    /* On small screens, the nav menu spans the full width of the screen. Leave a space for it. */\r\n    .body-content {\r\n        padding-top: 50px;\r\n    }\r\n}\r\n", ""]);

// exports


/***/ }),
/* 27 */
/***/ (function(module, exports, __webpack_require__) {

exports = module.exports = __webpack_require__(4)(undefined);
// imports


// module
exports.push([module.i, "@media (max-width: 767px) {\r\n    /* On small screens, the nav menu spans the full width of the screen. Leave a space for it. */\r\n    .body-content {\r\n        padding-top: 50px;\r\n    }\r\n}\r\n\r\n\r\n\r\n.mainHmDiv1 {\r\n    background: linear-gradient(to right, #3080c7, #3080c7 4%, #75e0e1 45%, #73e5e1 55%, #91d73a 97%);\r\n    background-repeat: no-repeat;\r\n}\r\n\r\n\r\n.mainHmDiv2 {\r\n    background: url(/images/xceed.tableview.glass.theme.png) no-repeat;\r\n    background-size: 100% 200px;\r\n    /*margin: 8px;*/\r\n    padding-left: 8px;\r\n\r\n    font-family: GeorgiaTimesNewRomanForLiningNumbers, \"Times New Roman\", Times, serif;\r\n    font-size: 130%;\r\n    line-height: 1.1;\r\n    color: #000000;\r\n}\r\n\r\n\r\n\r\n/*\r\nbody { background-color: transparent;  }\r\n\t\t.chart { \r\n\t\t\tmin-height:100px; height: 60vh; margin-bottom: 10px;\r\n\t        background: linear-gradient(#555, #333);\r\n            box-shadow: 3px 3px 15px rgba(0,0,0,0.6);\r\n            border-color: #4c4c4c;\r\n            margin: 0px 0px 2ex 1ex;\r\n            width: calc(100% - 3ex);\r\n\t\t}*/\r\n\r\n@font-face {\r\n    font-family: GeorgiaTimesNewRomanForLiningNumbers;\r\n    src: local(\"Georgia\");\r\n}\r\n\r\n@font-face {\r\n    font-family: GeorgiaTimesNewRomanForLiningNumbers;\r\n    src: local(\"Times New Roman\");\r\n    unicode-range: U+0030-0039;\r\n}\r\n\r\nbody {\r\n    font-family: GeorgiaTimesNewRomanForLiningNumbers, \"Times New Roman\", Times, serif;\r\n    font-size: 120%;\r\n    line-height: 1.1;\r\n}\r\n\r\nh3 {\r\n    margin-top: 0.2em;\r\n    margin-bottom: 0.1em;\r\n    font-size: 140%;\r\n    font-weight: bold;\r\n}\r\n\r\nh4 {\r\n    margin-top: 0.7em;\r\n    margin-bottom: 0.1em;\r\n    display: inline-block;\r\n    font-size: 120%;\r\n    font-weight: bold;\r\n}\r\n\r\nlabel {\r\n    margin-bottom: 0px;\r\n    font-weight: normal;\r\n}\r\n\r\n\r\n.headerBackground {\r\n    position: absolute;\r\n    top: 0;\r\n    left: 0;\r\n    width: 100%;\r\n    z-index: -1;\r\n    height: 33vh;\r\n    margin-top: 0;\r\n    margin-left: 0;\r\n    margin-right: 0;\r\n}\r\n\r\n.sqImportantOK {\r\n    font-size: 140%;\r\n    color: #10ff10;\r\n    font-weight: bold;\r\n}\r\n\r\n.sqImportantError {\r\n    font-size: 140%;\r\n    color: #FF2020;\r\n    font-weight: bold;\r\n}\r\n\r\n.lastChecksReportsDiv {\r\n    margin-top: 0.0em;\r\n    margin-bottom: 0.0em;\r\n}\r\n\r\n.lastChecksReportsUl {\r\n    margin-top: 0.0em;\r\n    margin-bottom: 0.0em;\r\n}\r\n\r\n.detailedReportsSection {\r\n    font-size: 75%;\r\n}\r\n\r\n.lastChecksDetailedReportsDiv {\r\n    margin-top: 0.0em;\r\n    margin-bottom: 0.0em;\r\n}\r\n\r\n.lastChecksDetailedReportsUl {\r\n    margin-top: 0.0em;\r\n    margin-bottom: 0.0em;\r\n}\r\n\r\n.loginUserDiv {\r\n    font-size: 70%;\r\n}\r\n\r\n.sqDebugInfo {\r\n    /*background-color: #79bbff;*/\r\n    font-size: 70%;\r\n    opacity: 0.5;\r\n    filter: alpha(opacity=50); /* msie */\r\n}\r\n\r\na {\r\n    padding: 5px;\r\n    text-decoration: none;\r\n}\r\n\r\n\r\n    a:visited, a:link {\r\n        color: #444;\r\n    }\r\n\r\n    a:hover {\r\n        color: white;\r\n        background-color: #1171a3;\r\n    }\r\n\r\n.sqhm-button {\r\n    -moz-box-shadow: 2px 2px 0px 0px #1564ad;\r\n    -webkit-box-shadow: 2px 2px 0px 0px #1564ad;\r\n    box-shadow: 2px 2px 0px 0px #1564ad;\r\n    background: -webkit-gradient(linear, left top, left bottom, color-stop(0.05, #79bbff), color-stop(1, #378de5));\r\n    background: -moz-linear-gradient(top, #79bbff 5%, #378de5 100%);\r\n    background: -webkit-linear-gradient(top, #79bbff 5%, #378de5 100%);\r\n    background: -o-linear-gradient(top, #79bbff 5%, #378de5 100%);\r\n    background: linear-gradient(to bottom, #79bbff 5%, #378de5 100%);\r\n    filter: progid:DXImageTransform.Microsoft.gradient(startColorstr='#79bbff', endColorstr='#378de5',GradientType=0);\r\n    background-color: #79bbff;\r\n    -moz-border-radius: 3px;\r\n    -webkit-border-radius: 3px;\r\n    border-radius: 3px;\r\n    border: 1px solid #337bc4;\r\n    display: inline-block;\r\n    cursor: pointer;\r\n    color: #ffffff;\r\n    font-family: Arial;\r\n    font-size: 14px;\r\n    font-weight: bold;\r\n    padding: 2px 14px;\r\n    text-decoration: none;\r\n    text-shadow: 0px 1px 0px #528ecc;\r\n}\r\n\r\n    .sqhm-button:hover {\r\n        background: -webkit-gradient(linear, left top, left bottom, color-stop(0.05, #378de5), color-stop(1, #79bbff));\r\n        background: -moz-linear-gradient(top, #378de5 5%, #79bbff 100%);\r\n        background: -webkit-linear-gradient(top, #378de5 5%, #79bbff 100%);\r\n        background: -o-linear-gradient(top, #378de5 5%, #79bbff 100%);\r\n        background: linear-gradient(to bottom, #378de5 5%, #79bbff 100%);\r\n        filter: progid:DXImageTransform.Microsoft.gradient(startColorstr='#378de5', endColorstr='#79bbff',GradientType=0);\r\n        background-color: #378de5;\r\n    }\r\n\r\n    .sqhm-button:active {\r\n        position: relative;\r\n        top: 1px;\r\n    }\r\n", ""]);

// exports


/***/ }),
/* 28 */
/***/ (function(module, exports, __webpack_require__) {

exports = module.exports = __webpack_require__(4)(undefined);
// imports


// module
exports.push([module.i, "li .glyphicon {\r\n    margin-right: 10px;\r\n}\r\n\r\n/* Highlighting rules for nav menu items */\r\nli.link-active a,\r\nli.link-active a:hover,\r\nli.link-active a:focus {\r\n    background-color: #4189C7;\r\n    color: white;\r\n}\r\n\r\n/* Keep the nav menu independent of scrolling and on top of other items */\r\n.main-nav {\r\n    position: fixed;\r\n    top: 0;\r\n    left: 0;\r\n    right: 0;\r\n    z-index: 1;\r\n}\r\n\r\n@media (min-width: 768px) {\r\n    /* On small screens, convert the nav menu to a vertical sidebar */\r\n    .main-nav {\r\n        height: 100%;\r\n        width: calc(25% - 20px);\r\n    }\r\n    .navbar {\r\n        border-radius: 0px;\r\n        border-width: 0px;\r\n        height: 100%;\r\n    }\r\n    .navbar-header {\r\n        float: none;\r\n    }\r\n    .navbar-collapse {\r\n        border-top: 1px solid #444;\r\n        padding: 0px;\r\n    }\r\n    .navbar ul {\r\n        float: none;\r\n    }\r\n    .navbar li {\r\n        float: none;\r\n        font-size: 15px;\r\n        margin: 6px;\r\n    }\r\n    .navbar li a {\r\n        padding: 10px 16px;\r\n        border-radius: 4px;\r\n    }\r\n    .navbar a {\r\n        /* If a menu item's text is too long, truncate it */\r\n        width: 100%;\r\n        white-space: nowrap;\r\n        overflow: hidden;\r\n        text-overflow: ellipsis;\r\n    }\r\n}\r\n", ""]);

// exports


/***/ }),
/* 29 */
/***/ (function(module, exports, __webpack_require__) {

exports = module.exports = __webpack_require__(4)(undefined);
// imports


// module
exports.push([module.i, "@media (max-width: 767px) {\r\n    /* On small screens, the nav menu spans the full width of the screen. Leave a space for it. */\r\n    .body-content {\r\n        padding-top: 50px;\r\n    }\r\n}\r\n\r\n\r\n.mainHmDiv1 {\r\n    /*background: linear-gradient(to right, #3080c7, #3080c7 4%, #75e0e1 45%, #73e5e1 55%, #91d73a 97%);\r\n    background-repeat: no-repeat;*/\r\n}\r\n\r\n.mainHmDiv2 {\r\n    /*background: url(/images/xceed.tableview.glass.theme.png) no-repeat;\r\n    background-size: 100% 200px;*/\r\n    margin: 8px;\r\n    /*padding-left: 8px;*/\r\n    font-family: GeorgiaTimesNewRomanForLiningNumbers, \"Times New Roman\", Times, serif;\r\n    font-size: 100%;\r\n    line-height: 1.1;\r\n    color: #333;\r\n    /*line-height: 1.1;*/\r\n    /*font-family: Arial;*/\r\n}\r\n\r\n\r\n\r\n@font-face {\r\n    font-family: GeorgiaTimesNewRomanForLiningNumbers;\r\n    src: local(\"Georgia\");\r\n}\r\n\r\n@font-face {\r\n    font-family: GeorgiaTimesNewRomanForLiningNumbers;\r\n    src: local(\"Times New Roman\");\r\n    unicode-range: U+0030-0039;\r\n}\r\n\r\n\r\n.divTitle {\r\n    width: 60%;\r\n    padding-top: 0px;\r\n    padding-bottom: 0px;\r\n    margin: 0 0 0 120px;\r\n    font-size: 130%; /*Sets the font-size to a percent of  the parent element's font size*/\r\n    font-weight: bold;\r\n}\r\n\r\n.spanTitleVersionNumber {\r\n    font-family: Arial;\r\n}\r\n\r\n\r\n.spanTitleSuperscript {\r\n    width: 20%;\r\n    margin: 0 auto;\r\n    font-size: x-small;\r\n    font-weight: normal;\r\n    vertical-align: super;\r\n}\r\n\r\n.spanStrategyDescription {\r\n    width: 20%;\r\n    margin: 0 auto;\r\n    font-weight: bold;\r\n    padding-left: 5px;\r\n    padding-right: 5px;\r\n    background: linear-gradient(#9EB5DE, #ffffff);\r\n}\r\n\r\n#googleHelpHref {\r\n    padding-left: 5px;\r\n    padding-right: 2px;\r\n}\r\n\r\n\r\n\r\n/*=== Navigation MainMenu ===*/\r\n\r\n.SqMenu2 {   /*ul li styles later can be specified as .SqMenu2 ul li { ... */ \r\n    margin: 0px;\r\n    padding: 0px;\r\n    list-style: none;\r\n}\r\n\r\n#idStartBacktestButtonLi {\r\n    width: 200px; /* width is the property of <li>*/\r\n    cursor: pointer;\r\n    font-size: 110%; /* Sets the font-size to a percent of  the parent element's font size */\r\n    font-weight: bold;\r\n    color: #ffea00;\r\n    box-shadow: inset 0 1px 0 0 #a5b9d9;\r\n}\r\n\r\n#idStartBacktestButtonLi :hover {\r\n    color: #10FF10;\r\n}\r\n\r\nul {\r\n    display: inline;\r\n    margin: 0;\r\n    padding: 0;\r\n}\r\n\r\n    ul li {\r\n        float: left; /* This assures that there are no gaps between li elements */\r\n        display: inline-block;\r\n        width: 100px;\r\n        background: linear-gradient(0deg, #012238, #2B7BC1);\r\n        color: #ffffff;\r\n        text-align: center;\r\n        border-radius: 4px;\r\n        border: 1px solid #111;\r\n        box-shadow: inset 0 1px 0 0 #a5b9d9;\r\n    }\r\n\r\n        ul li span {\r\n            display: block;     /* needed for div to fill an entire table cell */\r\n        }\r\n\r\n        ul li:hover {\r\n            color: #808080;\r\n            background: linear-gradient(0deg, #214258, #2B7BC1);\r\n        }\r\n\r\n            ul li:hover ul {\r\n                display: block;\r\n            }\r\n\r\n        ul li ul {\r\n            position: absolute;\r\n            width: 200px;\r\n            display: none;\r\n        }\r\n\r\n            ul li ul li {\r\n                background: linear-gradient(0deg, #012238, #2B7BC1);\r\n                width: 180px;\r\n                height: 27px;\r\n                cursor: pointer;\r\n                display: block;\r\n                align-items: center; /* align vertical, it only works if display is flex */\r\n            }\r\n\r\n                ul li ul li span {\r\n                    display: block; /* needed for div to fill an entire table cell */\r\n                    padding-top: 5px;\r\n                    padding-bottom: 5px;\r\n                }\r\n\r\n                ul li ul li:hover {\r\n                    background: linear-gradient(0deg, #214258, #2B7BC1);\r\n                }\r\n\r\n                    ul li ul li:hover ul {\r\n                        display: block;\r\n                    }\r\n\r\n                ul li ul li ul {\r\n                    position: relative;\r\n                    width: 200px;\r\n                    display: none;\r\n                    left: 179px;\r\n                    top: -25px;\r\n                    margin-left: 0px;\r\n                    list-style: none;\r\n                    padding: 0px;\r\n                }\r\n\r\n                    ul li ul li ul li {\r\n                        width: 220px;\r\n                        float: left;\r\n                        background: linear-gradient(0deg, #012238, #2B7BC1);\r\n                        height: 27px;\r\n                        cursor: pointer;\r\n                        display: block;\r\n                        align-items: center; /* align vertical, it only works if display is flex */\r\n                    }\r\n\r\n                        ul li ul li ul li span {\r\n                            display: block; /* needed for div to fill an entire table cell */\r\n                        }\r\n\r\n                        ul li ul li ul li:hover {\r\n                            background: linear-gradient(0deg, #214258, #2B7BC1);\r\n                        }\r\n\r\n\r\n\r\n\r\n.alwaysVisibleSwitchClass {\r\n    color: #0000EE; /* HTML5 recommended visited link colour */\r\n    text-decoration: underline;\r\n    cursor: pointer;\r\n    font-size: x-small;\r\n    font-weight: normal;\r\n}\r\n\r\n.xSmallComment {\r\n    font-size: x-small;\r\n    font-weight: normal;\r\n}\r\n\r\n\r\n\r\n\r\n#inputTable {\r\n    border-collapse: collapse;\r\n    border: solid black;\r\n    border-width: 1px 0px 1px 0;\r\n}\r\n\r\n    #inputTable tr.even {\r\n        background-color: #FEF6E0;\r\n    }\r\n\r\n    #inputTable th, td.colVIXF2per1 {\r\n        padding: 0px 7px 0px 7px;\r\n    }\r\n\r\n    #inputTable td {\r\n        text-align: center;\r\n    }\r\n\r\n    #inputTable th {\r\n        text-align: center;\r\n        padding-top: 0px;\r\n        padding-bottom: 0px;\r\n        /*background-color: #9EB5DE;*/\r\n        background: linear-gradient(#ffffff, #9EB5DE);\r\n    }\r\n\r\n    #inputTable .colDate, .colVIXF2per1, .colVIXF7per4per3 {\r\n        text-align: right;\r\n    }\r\n\r\n\r\n\r\n\r\n\r\n\r\n#statisticsTable {\r\n    border-collapse: collapse;\r\n    border: solid black;\r\n    border-width: 1px 0px 1px 0;\r\n}\r\n\r\n    #statisticsTable tr.even {\r\n        background-color: #FEF6E0;\r\n    }\r\n\r\n    #statisticsTable th, td.colVIXF2per1 {\r\n        padding: 0px 7px 0px 7px;\r\n    }\r\n\r\n    #statisticsTable td {\r\n        text-align: left;\r\n        padding-right: 10px;\r\n    }\r\n\r\n    #statisticsTable th {\r\n        text-align: center;\r\n        padding-top: 0px;\r\n        padding-bottom: 0px;\r\n        /*background-color: #9EB5DE;*/\r\n        background: linear-gradient(#ffffff, #9EB5DE);\r\n    }\r\n\r\n    #statisticsTable .colDate, .colVIXF2per1, .colVIXF7per4per3 {\r\n        text-align: right;\r\n    }\r\n\r\n\r\n#idSpanStatistics { /* At first, I thought I would use different font-types, like Verdana, Gothic, but Android and all tablets and Linux doesn't have these font-types. Better stay away from changing font-types at all */\r\n    font-weight: bold;\r\n    font-style: italic;\r\n}\r\n\r\n#statisticsTable #idTdGeneral {\r\n    font-weight: bold;\r\n    background-color: #ffffff;\r\n    padding-right: 80px;\r\n    font-style: italic;\r\n}\r\n\r\n#statisticsTable #idTdRebalanceFrequency {\r\n    background-color: #ffffff;\r\n    padding-left: 40px;\r\n    padding-right: 40px;\r\n}\r\n\r\n#statisticsTable #tdCagr {\r\n    font-weight: bold;\r\n    background-color: #BED5FE;\r\n}\r\n\r\n#statisticsTable #tdSharpe {\r\n    font-weight: bold;\r\n}\r\n\r\n\r\n\r\n.strategyNoteTable1 {   /* For example, TotM.cs strategy generates a custom Statistical table  HTML note as an output.*/\r\n    width: 90%; /*without it, IE is OK, but in Chrome, the Table resizes to huge after showing the invisible row */\r\n}\r\n\r\n    .strategyNoteTable1 th {\r\n        padding: 0px 7px 0px 7px;\r\n        background-color: #BED5FE;\r\n    }\r\n\r\n    .strategyNoteTable1 tr.even {\r\n        background-color: #FEF6E0;\r\n    }\r\n\r\n    .strategyNoteTable1 td {\r\n        text-align: center;\r\n    }\r\n\r\n        .strategyNoteTable1 td.red {\r\n            background-color: #f8696b\r\n        }\r\n\r\n        .strategyNoteTable1 td.green {\r\n            background-color: #63be7b\r\n        }\r\n", ""]);

// exports


/***/ }),
/* 30 */
/***/ (function(module, exports, __webpack_require__) {

module.exports = {
  XmlEntities: __webpack_require__(32),
  Html4Entities: __webpack_require__(31),
  Html5Entities: __webpack_require__(7),
  AllHtmlEntities: __webpack_require__(7)
};


/***/ }),
/* 31 */
/***/ (function(module, exports) {

var HTML_ALPHA = ['apos', 'nbsp', 'iexcl', 'cent', 'pound', 'curren', 'yen', 'brvbar', 'sect', 'uml', 'copy', 'ordf', 'laquo', 'not', 'shy', 'reg', 'macr', 'deg', 'plusmn', 'sup2', 'sup3', 'acute', 'micro', 'para', 'middot', 'cedil', 'sup1', 'ordm', 'raquo', 'frac14', 'frac12', 'frac34', 'iquest', 'Agrave', 'Aacute', 'Acirc', 'Atilde', 'Auml', 'Aring', 'Aelig', 'Ccedil', 'Egrave', 'Eacute', 'Ecirc', 'Euml', 'Igrave', 'Iacute', 'Icirc', 'Iuml', 'ETH', 'Ntilde', 'Ograve', 'Oacute', 'Ocirc', 'Otilde', 'Ouml', 'times', 'Oslash', 'Ugrave', 'Uacute', 'Ucirc', 'Uuml', 'Yacute', 'THORN', 'szlig', 'agrave', 'aacute', 'acirc', 'atilde', 'auml', 'aring', 'aelig', 'ccedil', 'egrave', 'eacute', 'ecirc', 'euml', 'igrave', 'iacute', 'icirc', 'iuml', 'eth', 'ntilde', 'ograve', 'oacute', 'ocirc', 'otilde', 'ouml', 'divide', 'oslash', 'ugrave', 'uacute', 'ucirc', 'uuml', 'yacute', 'thorn', 'yuml', 'quot', 'amp', 'lt', 'gt', 'OElig', 'oelig', 'Scaron', 'scaron', 'Yuml', 'circ', 'tilde', 'ensp', 'emsp', 'thinsp', 'zwnj', 'zwj', 'lrm', 'rlm', 'ndash', 'mdash', 'lsquo', 'rsquo', 'sbquo', 'ldquo', 'rdquo', 'bdquo', 'dagger', 'Dagger', 'permil', 'lsaquo', 'rsaquo', 'euro', 'fnof', 'Alpha', 'Beta', 'Gamma', 'Delta', 'Epsilon', 'Zeta', 'Eta', 'Theta', 'Iota', 'Kappa', 'Lambda', 'Mu', 'Nu', 'Xi', 'Omicron', 'Pi', 'Rho', 'Sigma', 'Tau', 'Upsilon', 'Phi', 'Chi', 'Psi', 'Omega', 'alpha', 'beta', 'gamma', 'delta', 'epsilon', 'zeta', 'eta', 'theta', 'iota', 'kappa', 'lambda', 'mu', 'nu', 'xi', 'omicron', 'pi', 'rho', 'sigmaf', 'sigma', 'tau', 'upsilon', 'phi', 'chi', 'psi', 'omega', 'thetasym', 'upsih', 'piv', 'bull', 'hellip', 'prime', 'Prime', 'oline', 'frasl', 'weierp', 'image', 'real', 'trade', 'alefsym', 'larr', 'uarr', 'rarr', 'darr', 'harr', 'crarr', 'lArr', 'uArr', 'rArr', 'dArr', 'hArr', 'forall', 'part', 'exist', 'empty', 'nabla', 'isin', 'notin', 'ni', 'prod', 'sum', 'minus', 'lowast', 'radic', 'prop', 'infin', 'ang', 'and', 'or', 'cap', 'cup', 'int', 'there4', 'sim', 'cong', 'asymp', 'ne', 'equiv', 'le', 'ge', 'sub', 'sup', 'nsub', 'sube', 'supe', 'oplus', 'otimes', 'perp', 'sdot', 'lceil', 'rceil', 'lfloor', 'rfloor', 'lang', 'rang', 'loz', 'spades', 'clubs', 'hearts', 'diams'];
var HTML_CODES = [39, 160, 161, 162, 163, 164, 165, 166, 167, 168, 169, 170, 171, 172, 173, 174, 175, 176, 177, 178, 179, 180, 181, 182, 183, 184, 185, 186, 187, 188, 189, 190, 191, 192, 193, 194, 195, 196, 197, 198, 199, 200, 201, 202, 203, 204, 205, 206, 207, 208, 209, 210, 211, 212, 213, 214, 215, 216, 217, 218, 219, 220, 221, 222, 223, 224, 225, 226, 227, 228, 229, 230, 231, 232, 233, 234, 235, 236, 237, 238, 239, 240, 241, 242, 243, 244, 245, 246, 247, 248, 249, 250, 251, 252, 253, 254, 255, 34, 38, 60, 62, 338, 339, 352, 353, 376, 710, 732, 8194, 8195, 8201, 8204, 8205, 8206, 8207, 8211, 8212, 8216, 8217, 8218, 8220, 8221, 8222, 8224, 8225, 8240, 8249, 8250, 8364, 402, 913, 914, 915, 916, 917, 918, 919, 920, 921, 922, 923, 924, 925, 926, 927, 928, 929, 931, 932, 933, 934, 935, 936, 937, 945, 946, 947, 948, 949, 950, 951, 952, 953, 954, 955, 956, 957, 958, 959, 960, 961, 962, 963, 964, 965, 966, 967, 968, 969, 977, 978, 982, 8226, 8230, 8242, 8243, 8254, 8260, 8472, 8465, 8476, 8482, 8501, 8592, 8593, 8594, 8595, 8596, 8629, 8656, 8657, 8658, 8659, 8660, 8704, 8706, 8707, 8709, 8711, 8712, 8713, 8715, 8719, 8721, 8722, 8727, 8730, 8733, 8734, 8736, 8743, 8744, 8745, 8746, 8747, 8756, 8764, 8773, 8776, 8800, 8801, 8804, 8805, 8834, 8835, 8836, 8838, 8839, 8853, 8855, 8869, 8901, 8968, 8969, 8970, 8971, 9001, 9002, 9674, 9824, 9827, 9829, 9830];

var alphaIndex = {};
var numIndex = {};

var i = 0;
var length = HTML_ALPHA.length;
while (i < length) {
    var a = HTML_ALPHA[i];
    var c = HTML_CODES[i];
    alphaIndex[a] = String.fromCharCode(c);
    numIndex[c] = a;
    i++;
}

/**
 * @constructor
 */
function Html4Entities() {}

/**
 * @param {String} str
 * @returns {String}
 */
Html4Entities.prototype.decode = function(str) {
    if (!str || !str.length) {
        return '';
    }
    return str.replace(/&(#?[\w\d]+);?/g, function(s, entity) {
        var chr;
        if (entity.charAt(0) === "#") {
            var code = entity.charAt(1).toLowerCase() === 'x' ?
                parseInt(entity.substr(2), 16) :
                parseInt(entity.substr(1));

            if (!(isNaN(code) || code < -32768 || code > 65535)) {
                chr = String.fromCharCode(code);
            }
        } else {
            chr = alphaIndex[entity];
        }
        return chr || s;
    });
};

/**
 * @param {String} str
 * @returns {String}
 */
Html4Entities.decode = function(str) {
    return new Html4Entities().decode(str);
};

/**
 * @param {String} str
 * @returns {String}
 */
Html4Entities.prototype.encode = function(str) {
    if (!str || !str.length) {
        return '';
    }
    var strLength = str.length;
    var result = '';
    var i = 0;
    while (i < strLength) {
        var alpha = numIndex[str.charCodeAt(i)];
        result += alpha ? "&" + alpha + ";" : str.charAt(i);
        i++;
    }
    return result;
};

/**
 * @param {String} str
 * @returns {String}
 */
Html4Entities.encode = function(str) {
    return new Html4Entities().encode(str);
};

/**
 * @param {String} str
 * @returns {String}
 */
Html4Entities.prototype.encodeNonUTF = function(str) {
    if (!str || !str.length) {
        return '';
    }
    var strLength = str.length;
    var result = '';
    var i = 0;
    while (i < strLength) {
        var cc = str.charCodeAt(i);
        var alpha = numIndex[cc];
        if (alpha) {
            result += "&" + alpha + ";";
        } else if (cc < 32 || cc > 126) {
            result += "&#" + cc + ";";
        } else {
            result += str.charAt(i);
        }
        i++;
    }
    return result;
};

/**
 * @param {String} str
 * @returns {String}
 */
Html4Entities.encodeNonUTF = function(str) {
    return new Html4Entities().encodeNonUTF(str);
};

/**
 * @param {String} str
 * @returns {String}
 */
Html4Entities.prototype.encodeNonASCII = function(str) {
    if (!str || !str.length) {
        return '';
    }
    var strLength = str.length;
    var result = '';
    var i = 0;
    while (i < strLength) {
        var c = str.charCodeAt(i);
        if (c <= 255) {
            result += str[i++];
            continue;
        }
        result += '&#' + c + ';';
        i++;
    }
    return result;
};

/**
 * @param {String} str
 * @returns {String}
 */
Html4Entities.encodeNonASCII = function(str) {
    return new Html4Entities().encodeNonASCII(str);
};

module.exports = Html4Entities;


/***/ }),
/* 32 */
/***/ (function(module, exports) {

var ALPHA_INDEX = {
    '&lt': '<',
    '&gt': '>',
    '&quot': '"',
    '&apos': '\'',
    '&amp': '&',
    '&lt;': '<',
    '&gt;': '>',
    '&quot;': '"',
    '&apos;': '\'',
    '&amp;': '&'
};

var CHAR_INDEX = {
    60: 'lt',
    62: 'gt',
    34: 'quot',
    39: 'apos',
    38: 'amp'
};

var CHAR_S_INDEX = {
    '<': '&lt;',
    '>': '&gt;',
    '"': '&quot;',
    '\'': '&apos;',
    '&': '&amp;'
};

/**
 * @constructor
 */
function XmlEntities() {}

/**
 * @param {String} str
 * @returns {String}
 */
XmlEntities.prototype.encode = function(str) {
    if (!str || !str.length) {
        return '';
    }
    return str.replace(/<|>|"|'|&/g, function(s) {
        return CHAR_S_INDEX[s];
    });
};

/**
 * @param {String} str
 * @returns {String}
 */
 XmlEntities.encode = function(str) {
    return new XmlEntities().encode(str);
 };

/**
 * @param {String} str
 * @returns {String}
 */
XmlEntities.prototype.decode = function(str) {
    if (!str || !str.length) {
        return '';
    }
    return str.replace(/&#?[0-9a-zA-Z]+;?/g, function(s) {
        if (s.charAt(1) === '#') {
            var code = s.charAt(2).toLowerCase() === 'x' ?
                parseInt(s.substr(3), 16) :
                parseInt(s.substr(2));

            if (isNaN(code) || code < -32768 || code > 65535) {
                return '';
            }
            return String.fromCharCode(code);
        }
        return ALPHA_INDEX[s] || s;
    });
};

/**
 * @param {String} str
 * @returns {String}
 */
 XmlEntities.decode = function(str) {
    return new XmlEntities().decode(str);
 };

/**
 * @param {String} str
 * @returns {String}
 */
XmlEntities.prototype.encodeNonUTF = function(str) {
    if (!str || !str.length) {
        return '';
    }
    var strLength = str.length;
    var result = '';
    var i = 0;
    while (i < strLength) {
        var c = str.charCodeAt(i);
        var alpha = CHAR_INDEX[c];
        if (alpha) {
            result += "&" + alpha + ";";
            i++;
            continue;
        }
        if (c < 32 || c > 126) {
            result += '&#' + c + ';';
        } else {
            result += str.charAt(i);
        }
        i++;
    }
    return result;
};

/**
 * @param {String} str
 * @returns {String}
 */
 XmlEntities.encodeNonUTF = function(str) {
    return new XmlEntities().encodeNonUTF(str);
 };

/**
 * @param {String} str
 * @returns {String}
 */
XmlEntities.prototype.encodeNonASCII = function(str) {
    if (!str || !str.length) {
        return '';
    }
    var strLenght = str.length;
    var result = '';
    var i = 0;
    while (i < strLenght) {
        var c = str.charCodeAt(i);
        if (c <= 255) {
            result += str[i++];
            continue;
        }
        result += '&#' + c + ';';
        i++;
    }
    return result;
};

/**
 * @param {String} str
 * @returns {String}
 */
 XmlEntities.encodeNonASCII = function(str) {
    return new XmlEntities().encodeNonASCII(str);
 };

module.exports = XmlEntities;


/***/ }),
/* 33 */
/***/ (function(module, exports) {

module.exports = "\r\n<div>\r\n    <router-outlet></router-outlet>\r\n</div>\r\n\r\n<!--<div class='container-fluid'>\r\n    <div class='row'>\r\n        <div class='col-sm-3'>\r\n            <nav-menu></nav-menu>\r\n        </div>\r\n        <div class='col-sm-9 body-content'>\r\n            <router-outlet></router-outlet>\r\n        </div>\r\n    </div>\r\n</div>-->\r\n\r\n\r\n<br /><p><small>DebugInfo: window.sqWebAppName: <strong>'{{ g_sqWebAppName }}'</strong></small></p>";

/***/ }),
/* 34 */
/***/ (function(module, exports) {

module.exports = "<h1>Counter</h1>\r\n\r\n<p>This is a simple example of an Angular component.</p>\r\n\r\n<p>Current count: <strong>{{ currentCount }}</strong></p>\r\n\r\n<button (click)=\"incrementCounter()\">Increment</button>\r\n";

/***/ }),
/* 35 */
/***/ (function(module, exports) {

module.exports = "<h1>Weather forecast</h1>\r\n\r\n<p>This component demonstrates fetching data from the server.</p>\r\n\r\n<p *ngIf=\"!forecasts\"><em>Loading...</em></p>\r\n\r\n<table class='table' *ngIf=\"forecasts\">\r\n    <thead>\r\n        <tr>\r\n            <th>Date</th>\r\n            <th>Temp. (C)</th>\r\n            <th>Temp. (F)</th>\r\n            <th>Summary</th>\r\n        </tr>\r\n    </thead>\r\n    <tbody>\r\n        <tr *ngFor=\"let forecast of forecasts\">\r\n            <td>{{ forecast.dateFormatted }}</td>\r\n            <td>{{ forecast.temperatureC }}</td>\r\n            <td>{{ forecast.temperatureF }}</td>\r\n            <td>{{ forecast.summary }}</td>\r\n        </tr>\r\n    </tbody>\r\n</table>\r\n";

/***/ }),
/* 36 */
/***/ (function(module, exports) {

module.exports = "<!--<img class=\"headerBackground\" src=\"/images/xceed.tableview.glass.theme.png\" />-->\r\n<!--<div style=\"position:absolute;top: 0px; \">-->\r\n<div class=\"mainHmDiv1\">\r\n    <div class=\"mainHmDiv2\">\r\n        <h3>{{m_title}} &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;v. 0.2.49</h3>\r\n\r\n\r\n        <h4>1. Main HealthMonitor App</h4>\r\n\r\n        <span class=\"sqImportantError\"> &nbsp; <span *ngIf=\"m_data\" [class.sqImportantOK]=\"m_data.AppOk === 'OK'\">{{m_data.AppOk}}</span></span><br />\r\n        Response to FrontEnd:&nbsp; <span *ngIf=\"m_data\">{{m_data.ResponseToFrontEnd}}</span> <br />\r\n        Started on <span *ngIf=\"m_data\">{{m_data.StartDate}} &nbsp;&nbsp; {{m_data.StartDateTimeSpanStr}} </span>\r\n        <div>\r\n            <label>\r\n                Daily Email Report active: <span *ngIf=\"m_data\"><input #chkDailyEmail type=\"checkbox\" [checked]=\"m_data.DailyEmailReportEnabled\" (change)=\"setControlValue('chkDailyEmail', chkDailyEmail.checked)\" /></span>\r\n            </label>\r\n        </div>\r\n\r\n        <h4>2. Realtime Price Service</h4>\r\n        <span class=\"sqImportantError\"> &nbsp; <span *ngIf=\"m_data\" [class.sqImportantOK]=\"m_data.RtpsOk === 'OK'\">{{m_data.RtpsOk}}</span></span>\r\n        <div>\r\n            <label>\r\n                Realtime Price Service check timer active: <span *ngIf=\"m_data\"><input #chkRtps type=\"checkbox\" [checked]=\"m_data.RtpsTimerEnabled\" (change)=\"setControlValue('chkRtps', chkRtps.checked)\" /></span>\r\n            </label>\r\n        </div>\r\n        Timer frequency: every <span *ngIf=\"m_data\">{{m_data.RtpsTimerFrequencyMinutes}} </span>minutes. Last checks:<br />\r\n        <div class=\"lastChecksReportsDiv\" *ngIf=\"m_data\">\r\n            <ul class=\"lastChecksReportsUl\">\r\n                <li *ngFor=\"let rtps of m_data.RtpsDownloads\">\r\n                    <span>{{rtps}}</span>\r\n                </li>\r\n            </ul>\r\n        </div>\r\n        <h4>3. VBroker 1.0</h4>\r\n        <span class=\"sqImportantError\"> &nbsp; <span *ngIf=\"m_data\" [class.sqImportantOK]=\"m_data.VBrokerOk === 'OK'\">{{m_data.VBrokerOk}}</span></span>\r\n        <div>\r\n            <label>\r\n                Processing VBroker messages active: <span *ngIf=\"m_data\"><input #chkVBroker type=\"checkbox\" [checked]=\"m_data.ProcessingVBrokerMessagesEnabled\" (change)=\"setControlValue('chkVBroker', chkVBroker.checked)\" /></span>\r\n            </label>\r\n        </div>\r\n        No timer for active polling. Last received messages:<br />\r\n        <div class=\"lastChecksReportsDiv\" *ngIf=\"m_data\">\r\n            <ul class=\"lastChecksReportsUl\">\r\n                <li *ngFor=\"let report of m_data.VBrokerReports\">\r\n                    <span>{{report}}</span>\r\n                </li>\r\n            </ul>\r\n        </div>\r\n\r\n        <br />\r\n        <button class=\"sqhm-button\" (click)=\"refreshDataClicked()\">Refresh Data</button><br />\r\n        <span class=\"loginUserDiv\">Logged in as {{m_userEmail}};</span>\r\n        <hr>\r\n        <div class=\"detailedReportsSection\">\r\n            <strong>VBroker Detailed</strong><br />\r\n            <div class=\"lastChecksDetailedReportsDiv\" *ngIf=\"m_data\">\r\n                <ul class=\"lastChecksDetailedReportsUl\">\r\n                    <li *ngFor=\"let report of m_data.VBrokerDetailedReports\">\r\n                        <div [innerHTML]=\"report\"></div>\r\n                    </li>\r\n                </ul>\r\n            </div>\r\n        </div>\r\n        <br /><br /><br />\r\n        Click <a href=\"/DeveloperDashboard\">here for Developer Dashboard</a>  (Or for more VBroker details Developers can peek the Linux terminal.)<br /><br />\r\n        <div class=\"sqDebugInfo\">\r\n            <div>**************** Debug area for developers *************</div>\r\n\r\n            WebAppResponse: {{m_webAppResponse}}<br /><br />\r\n            Refresh button: {{m_wasRefreshClicked}}<br /><br />\r\n        </div>\r\n    </div>\r\n</div>\r\n";

/***/ }),
/* 37 */
/***/ (function(module, exports) {

module.exports = "<h1>Hello, world!</h1>\r\n<p>Welcome to your new single-page application, built with:</p>\r\n<ul>\r\n    <li><a href='https://get.asp.net/'>ASP.NET Core</a> and <a href='https://msdn.microsoft.com/en-us/library/67ef8sbd.aspx'>C#</a> for cross-platform server-side code</li>\r\n    <li><a href='https://angular.io/'>Angular</a> and <a href='http://www.typescriptlang.org/'>TypeScript</a> for client-side code</li>\r\n    <li><a href='https://webpack.github.io/'>Webpack</a> for building and bundling client-side resources</li>\r\n    <li><a href='http://getbootstrap.com/'>Bootstrap</a> for layout and styling</li>\r\n</ul>\r\n<p>To help you get started, we've also set up:</p>\r\n<ul>\r\n    <li><strong>Client-side navigation</strong>. For example, click <em>Counter</em> then <em>Back</em> to return here.</li>\r\n    <li><strong>Server-side prerendering</strong>. For faster initial loading and improved SEO, your Angular app is prerendered on the server. The resulting HTML is then transferred to the browser where a client-side copy of the app takes over.</li>\r\n    <li><strong>Webpack dev middleware</strong>. In development mode, there's no need to run the <code>webpack</code> build tool. Your client-side resources are dynamically built on demand. Updates are available as soon as you modify any file.</li>\r\n    <li><strong>Hot module replacement</strong>. In development mode, you don't even need to reload the page after making most changes. Within seconds of saving changes to files, your Angular app will be rebuilt and a new instance injected into the page.</li>\r\n    <li><strong>Efficient production builds</strong>. In production mode, development-time features are disabled, and the <code>webpack</code> build tool produces minified static CSS and JavaScript files.</li>\r\n</ul>\r\n";

/***/ }),
/* 38 */
/***/ (function(module, exports) {

module.exports = "<div class='main-nav'>\r\n    <div class='navbar navbar-inverse'>\r\n        <div class='navbar-header'>\r\n            <button type='button' class='navbar-toggle' data-toggle='collapse' data-target='.navbar-collapse'>\r\n                <span class='sr-only'>Toggle navigation</span>\r\n                <span class='icon-bar'></span>\r\n                <span class='icon-bar'></span>\r\n                <span class='icon-bar'></span>\r\n            </button>\r\n            <a class='navbar-brand' [routerLink]=\"['/home']\">SQLab</a>\r\n        </div>\r\n        <div class='clearfix'></div>\r\n        <div class='navbar-collapse collapse'>\r\n            <ul class='nav navbar-nav'>\r\n                <li [routerLinkActive]=\"['link-active']\">\r\n                    <a [routerLink]=\"['/home']\">\r\n                        <span class='glyphicon glyphicon-home'></span> Home\r\n                    </a>\r\n                </li>\r\n                <li [routerLinkActive]=\"['link-active']\">\r\n                    <a [routerLink]=\"['/counter']\">\r\n                        <span class='glyphicon glyphicon-education'></span> Counter\r\n                    </a>\r\n                </li>\r\n                <li [routerLinkActive]=\"['link-active']\">\r\n                    <a [routerLink]=\"['/healthmonitor']\">\r\n                        <span class='glyphicon glyphicon-education'></span> HealthMonitor\r\n                    </a>\r\n                </li>\r\n                <li [routerLinkActive]=\"['link-active']\">\r\n                    <a [routerLink]=\"['/fetch-data']\">\r\n                        <span class='glyphicon glyphicon-th-list'></span> Fetch data\r\n                    </a>\r\n                </li>\r\n            </ul>\r\n        </div>\r\n    </div>\r\n</div>\r\n";

/***/ }),
/* 39 */
/***/ (function(module, exports) {

module.exports = "<!--<img class=\"headerBackground\" src=\"/images/xceed.tableview.glass.theme.png\" />-->\r\n<!--<div style=\"position:absolute;top: 0px; \">-->\r\n<div class=\"mainHmDiv1\">\r\n    <div class=\"mainHmDiv2\">\r\n        <div class=\"divTitle\">SQ Strategy QuickTester (<span class=\"spanTitleVersionNumber\">{{m_versionShortInfo}}</span>) <span class=\"spanTitleSuperscript\">*RealTime</span> </div>\r\n        <div style=\"width: 100%; height: 2px; background: #ffffff; overflow: hidden;\"></div>\r\n        <!--//http://jsfiddle.net/rabidGadfly/f8ea6/ this was a good Angular menu, but submenus was not hovering-->\r\n        <!--// jquery horizontal menu is from here (I gave up finding Angular one): http://runnable.com/UdQdRyHniSpKAAXx/create-a-horizontal-navigation-menu-bar-using-jquery-->\r\n        <!-- Use this navigation div as your menu bar div -->\r\n        <div class=\"SqMenu2\">\r\n            <ul>\r\n                <li id=\"idStartBacktestButtonLi\">\r\n                    <span class=\"startBacktestButton\" (click)=\"MenuItemStartBacktestClicked()\">Start Backtest!</span>\r\n                </li>\r\n                <li>\r\n                    <span>Tools</span>\r\n                    <ul>\r\n                        <li><span>Copy PV to clipboard</span></li>\r\n                        <li><span (click)=\"MenuItemVersionInfoClicked()\">Version info</span></li>\r\n                    </ul>\r\n                </li>\r\n                <li>\r\n                    <span>Debug</span>\r\n                    <ul>\r\n                        <li><span>See debug info 1</span></li>\r\n                        <li><span>See debug info 2</span></li>\r\n                    </ul>\r\n                </li>\r\n                <li>\r\n                    <span>Strategies</span>\r\n                    <ul>\r\n                        <li>\r\n                            <span>Adaptive</span>\r\n                            <ul>\r\n                                <li><span id=\"idMenuItemAdaptiveUberVxx\" (click)=\"MenuItemStrategyClick($event)\">UberVxx</span></li>\r\n                            </ul>\r\n                        </li>\r\n                        <li>\r\n                            <span>Seasonalities</span>\r\n                            <ul>\r\n                                <li><span id=\"idMenuItemTotM\" (click)=\"MenuItemStrategyClick($event)\">Turn of the Month</span></li>\r\n                            </ul>\r\n                        </li>\r\n                        <li>\r\n                            <span>VIX</span>\r\n                            <ul>\r\n                                <li><span id=\"idMenuItemVXX_SPY_Controversial\" (click)=\"MenuItemStrategyClick($event)\">XIV width VXX-SPY Cont.Day</span></li>\r\n                            </ul>\r\n                        </li>\r\n                        <li>\r\n                            <span>L-ETF</span>\r\n                            <ul>\r\n                                <li><span id=\"idMenuItemLETFDiscrRebToNeutral\" (click)=\"MenuItemStrategyClick($event)\">L-ETF Discr. Reb.ToNeutral</span></li>\r\n                                <li><span id=\"idMenuItemLETFDiscrAddToWinner\" (click)=\"MenuItemStrategyClick($event)\">L-ETF Discr. AddToWinning</span></li>\r\n                                <li><span id=\"idMenuItemLETFHarryLong\" (click)=\"MenuItemStrategyClick($event)\">Harry Long</span></li>\r\n                            </ul>\r\n                        </li>\r\n                        <li>\r\n                            <span>Asset Allocation</span>\r\n                            <ul>\r\n                                <li><span id=\"idMenuItemTAA\" (click)=\"MenuItemStrategyClick($event)\">Tactical AA (TAA)</span></li>\r\n                            </ul>\r\n                        </li>\r\n                    </ul>\r\n                </li>\r\n            </ul>\r\n        </div>\r\n        <br>\r\n        <div style=\"width: 100%; height: 1px; background: #808080; overflow: hidden;\"></div>\r\n\r\n\r\n\r\n        <span id=\"idAlwaysVisibleSwitch0\" class=\"alwaysVisibleSwitchClass\" (click)=\"SQToggle('idGeneralParameters', 'idAlwaysVisibleSwitch0', 'General Parameters')\">+ Show General Parameters</span>\r\n        <br />\r\n        <div id=\"idGeneralParameters\" style=\"display: none;\">\r\n            <table id=\"idGeneralParametersTable\">\r\n                <colgroup>\r\n                    <col class=\"column1\" />\r\n                    <col class=\"column2\" />\r\n                </colgroup>\r\n                <thead>\r\n                    <tr>\r\n                        <th>StartDateUtc*</th>\r\n                        <th>EndDateUtc*</th>\r\n                        <th class=\"xSmallComment\">*(UTC-16:00 will be converted to Exchange Time Zone)</th>\r\n                    </tr>\r\n                </thead>\r\n                <tbody>\r\n                    <tr>\r\n                        <td><input type=\"text\" [(ngModel)]=\"inputStartDateStr\" style=\"width: 100px; padding: 1px; border: 1px solid #A0A0A0\" /></td>\r\n                        <td><input type=\"text\" [(ngModel)]=\"inputEndDateStr\" style=\"width: 100px; padding: 1px; border: 1px solid #A0A0A0\" /></td>\r\n                    </tr>\r\n                </tbody>\r\n            </table>\r\n        </div>\r\n\r\n\r\n\r\n\r\n        <span>SelectedStrategy:</span>\r\n        <span class=\"spanStrategyDescription\"> {{selectedSubStrategyName}} </span>\r\n        <a id=\"googleHelpHref\" target=\"_blank\" href=\"{{selectedSubStrategyHelpUri}}\"> Help</a>\r\n\r\n        <span class=\"spanTitleSuperscript\">(GoogleDoc)</span>\r\n        <div style=\"height: 1px; background: #808080; overflow: hidden;\"></div>\r\n        <!--<div style=\"width: 50%; height: 1px; background: #ffffff; overflow: hidden;\"></div>--> <!--extra 1 pixel line, so that the input rows doesn't touch each other -->    <!--// fast SSI, Server Side Include works in Azure website from 2014: http://azure.microsoft.com/blog/2014/03/26/server-side-includes-ssi-in-windows-azure-web-sites-waws/-->\r\n        <!--#include virtual=\"/SQQuickTester/Strategies/AdaptiveUberVXX/AdaptiveUberVXX.htm\"-->\r\n        <div [hidden]=\"selectedSubStrategyMenuItemId !== 'idMenuItemAdaptiveUberVxx'\">\r\n            <table id=\"inputTable\" align=\"left\">\r\n                <colgroup>\r\n                    <col class=\"column1\" />\r\n                    <col class=\"column2\" />\r\n                </colgroup>\r\n                <thead>\r\n                    <tr>\r\n                        <th>Trading instrument on bullish day</th>\r\n                        <th>Param</th>\r\n                    </tr>\r\n                </thead>\r\n                <tbody>\r\n                    <tr>\r\n                        <td>\r\n                            <select [ngModel]=\"strategy_AdaptiveUberVxx.selectedBullishTradingInstrument\" (ngModelChange)=\"strategy_AdaptiveUberVxx.bullishTradingInstrumentChanged($event)\">\r\n                                <option [value]=\"i\" *ngFor=\"let i of strategy_AdaptiveUberVxx.bullishTradingInstrument\">{{i}}</option>\r\n                            </select>\r\n                        <td><input type=\"text\" [(ngModel)]=\"strategy_AdaptiveUberVxx.param\" style=\"width: 280px; padding: 2px; border: 1px solid #A0A0A0\" (keypress)=\"OnParameterInputKeypress($event)\" />                        </td>\r\n                    </tr>\r\n                </tbody>\r\n            </table>\r\n            <table id=\"inputTable\" align=\"left\">\r\n                <colgroup>\r\n                    <col class=\"column1\" />\r\n                    <col class=\"column2\" />\r\n                </colgroup>\r\n                <thead>\r\n                    <tr>\r\n                        <th>Component</th>\r\n                        <th>Priority</th>\r\n                        <th>Combination</th>\r\n                        <th>StartDate</th>\r\n                        <th>EndDate</th>\r\n                        <th>TradingStartAt</th>\r\n                        <th>Param</th>\r\n                    </tr>\r\n                </thead>\r\n                <tbody>\r\n                    <tr>\r\n                        <td>{{strategy_AdaptiveUberVxx.fomc.Name}}</td>\r\n                        <td><input type=\"text\" [(ngModel)]=\"strategy_AdaptiveUberVxx.fomc.Priority\" style=\"width: 20px; padding: 2px; border: 1px solid #A0A0A0\" (keypress)=\"OnParameterInputKeypress($event)\" /></td>\r\n                        <td><input type=\"text\" [(ngModel)]=\"strategy_AdaptiveUberVxx.fomc.Combination\" style=\"width: 60px; padding: 2px; border: 1px solid #A0A0A0\" (keypress)=\"OnParameterInputKeypress($event)\" /></td>\r\n                        <td><input type=\"text\" [(ngModel)]=\"strategy_AdaptiveUberVxx.fomc.StartDate\" style=\"width: 60px; padding: 2px; border: 1px solid #A0A0A0\" (keypress)=\"OnParameterInputKeypress($event)\" /></td>\r\n                        <td><input type=\"text\" [(ngModel)]=\"strategy_AdaptiveUberVxx.fomc.EndDate\" style=\"width: 60px; padding: 2px; border: 1px solid #A0A0A0\" (keypress)=\"OnParameterInputKeypress($event)\" /></td>\r\n                        <td><input type=\"text\" [(ngModel)]=\"strategy_AdaptiveUberVxx.fomc.TradingStartAt\" style=\"width: 40px; padding: 2px; border: 1px solid #A0A0A0\" (keypress)=\"OnParameterInputKeypress($event)\" /></td>\r\n                        <td><input type=\"text\" [(ngModel)]=\"strategy_AdaptiveUberVxx.fomc.Param\" style=\"width: 200px; padding: 2px; border: 1px solid #A0A0A0\" (keypress)=\"OnParameterInputKeypress($event)\" /></td>\r\n                    </tr>\r\n                    <tr>\r\n                        <td>{{strategy_AdaptiveUberVxx.holiday.Name}}</td>\r\n                        <td><input type=\"text\" [(ngModel)]=\"strategy_AdaptiveUberVxx.holiday.Priority\" style=\"width: 20px; padding: 2px; border: 1px solid #A0A0A0\" (keypress)=\"OnParameterInputKeypress($event)\" /></td>\r\n                        <td><input type=\"text\" [(ngModel)]=\"strategy_AdaptiveUberVxx.holiday.Combination\" style=\"width: 60px; padding: 2px; border: 1px solid #A0A0A0\" (keypress)=\"OnParameterInputKeypress($event)\" /></td>\r\n                        <td><input type=\"text\" [(ngModel)]=\"strategy_AdaptiveUberVxx.holiday.StartDate\" style=\"width: 60px; padding: 2px; border: 1px solid #A0A0A0\" (keypress)=\"OnParameterInputKeypress($event)\" /></td>\r\n                        <td><input type=\"text\" [(ngModel)]=\"strategy_AdaptiveUberVxx.holiday.EndDate\" style=\"width: 60px; padding: 2px; border: 1px solid #A0A0A0\" (keypress)=\"OnParameterInputKeypress($event)\" /></td>\r\n                        <td><input type=\"text\" [(ngModel)]=\"strategy_AdaptiveUberVxx.holiday.TradingStartAt\" style=\"width: 40px; padding: 2px; border: 1px solid #A0A0A0\" (keypress)=\"OnParameterInputKeypress($event)\" /></td>\r\n                        <td><input type=\"text\" [(ngModel)]=\"strategy_AdaptiveUberVxx.holiday.Param\" style=\"width: 200px; padding: 2px; border: 1px solid #A0A0A0\" (keypress)=\"OnParameterInputKeypress($event)\" /></td>\r\n                    </tr>\r\n                    <tr>\r\n                        <td>{{strategy_AdaptiveUberVxx.totm.Name}}</td>\r\n                        <td><input type=\"text\" [(ngModel)]=\"strategy_AdaptiveUberVxx.totm.Priority\" style=\"width: 20px; padding: 2px; border: 1px solid #A0A0A0\" (keypress)=\"OnParameterInputKeypress($event)\" /></td>\r\n                        <td><input type=\"text\" [(ngModel)]=\"strategy_AdaptiveUberVxx.totm.Combination\" style=\"width: 60px; padding: 2px; border: 1px solid #A0A0A0\" (keypress)=\"OnParameterInputKeypress($event)\" /></td>\r\n                        <td><input type=\"text\" [(ngModel)]=\"strategy_AdaptiveUberVxx.totm.StartDate\" style=\"width: 60px; padding: 2px; border: 1px solid #A0A0A0\" (keypress)=\"OnParameterInputKeypress($event)\" /></td>\r\n                        <td><input type=\"text\" [(ngModel)]=\"strategy_AdaptiveUberVxx.totm.EndDate\" style=\"width: 60px; padding: 2px; border: 1px solid #A0A0A0\" (keypress)=\"OnParameterInputKeypress($event)\" /></td>\r\n                        <td><input type=\"text\" [(ngModel)]=\"strategy_AdaptiveUberVxx.totm.TradingStartAt\" style=\"width: 40px; padding: 2px; border: 1px solid #A0A0A0\" (keypress)=\"OnParameterInputKeypress($event)\" /></td>\r\n                        <td><input type=\"text\" [(ngModel)]=\"strategy_AdaptiveUberVxx.totm.Param\" style=\"width: 200px; padding: 2px; border: 1px solid #A0A0A0\" (keypress)=\"OnParameterInputKeypress($event)\" /></td>\r\n                    </tr>\r\n                    <tr>\r\n                        <td>{{strategy_AdaptiveUberVxx.connor.Name}}</td>\r\n                        <td><input type=\"text\" [(ngModel)]=\"strategy_AdaptiveUberVxx.connor.Priority\" style=\"width: 20px; padding: 2px; border: 1px solid #A0A0A0\" (keypress)=\"OnParameterInputKeypress($event)\" /></td>\r\n                        <td><input type=\"text\" [(ngModel)]=\"strategy_AdaptiveUberVxx.connor.Combination\" style=\"width: 60px; padding: 2px; border: 1px solid #A0A0A0\" (keypress)=\"OnParameterInputKeypress($event)\" /></td>\r\n                        <td><input type=\"text\" [(ngModel)]=\"strategy_AdaptiveUberVxx.connor.StartDate\" style=\"width: 60px; padding: 2px; border: 1px solid #A0A0A0\" (keypress)=\"OnParameterInputKeypress($event)\" /></td>\r\n                        <td><input type=\"text\" [(ngModel)]=\"strategy_AdaptiveUberVxx.connor.EndDate\" style=\"width: 60px; padding: 2px; border: 1px solid #A0A0A0\" (keypress)=\"OnParameterInputKeypress($event)\" /></td>\r\n                        <td><input type=\"text\" [(ngModel)]=\"strategy_AdaptiveUberVxx.connor.TradingStartAt\" style=\"width: 40px; padding: 2px; border: 1px solid #A0A0A0\" (keypress)=\"OnParameterInputKeypress($event)\" /></td>\r\n                        <td><input type=\"text\" [(ngModel)]=\"strategy_AdaptiveUberVxx.connor.Param\" style=\"width: 200px; padding: 2px; border: 1px solid #A0A0A0\" (keypress)=\"OnParameterInputKeypress($event)\" /></td>\r\n                    </tr>\r\n                </tbody>\r\n            </table>\r\n        </div>\r\n\r\n        <!--#include virtual=\"/SQQuickTester/Strategies/TotM/TotM.htm\"-->\r\n        <table id=\"inputTable\" [hidden]=\"selectedSubStrategyMenuItemId !== 'idMenuItemTotM'\" align=\"left\">\r\n            <colgroup>\r\n                <col class=\"column1\" />\r\n                <col class=\"column2\" />\r\n            </colgroup>\r\n            <thead>\r\n                <tr>\r\n                    <th>Trading instrument<br />on bullish day</th>\r\n                    <th>Daily mask, Winter<br />(TotM, rel.to 1st day)</th>\r\n                    <th>Daily mask, Winter<br />(TotMM, rel.to 15th day)</th>\r\n                    <th>Daily mask, Summer<br />(TotM, rel.to 1st day)</th>\r\n                    <th>Daily mask, Summer<br />(TotMM, rel.to 15th day)</th>\r\n                </tr>\r\n            </thead>\r\n            <tbody>\r\n                <tr>\r\n                    <td>\r\n                        <select [ngModel]=\"strategy_TotM.selectedBullishTradingInstrument\" (ngModelChange)=\"strategy_TotM.bullishTradingInstrumentChanged($event)\">\r\n                            <option [value]=\"i\" *ngFor=\"let i of strategy_TotM.bullishTradingInstrument\">{{i}}</option>\r\n                        </select>\r\n\r\n                    </td>\r\n                    <td><input type=\"text\" [(ngModel)]=\"strategy_TotM.dailyMarketDirectionMaskWinterTotM\" style=\"width: 140px; padding: 2px; border: 1px solid #A0A0A0\" (keypress)=\"OnParameterInputKeypress($event)\" /></td>\r\n                    <td><input type=\"text\" [(ngModel)]=\"strategy_TotM.dailyMarketDirectionMaskWinterTotMM\" style=\"width: 140px; padding: 2px; border: 1px solid #A0A0A0\" (keypress)=\"OnParameterInputKeypress($event)\" /></td>\r\n                    <td><input type=\"text\" [(ngModel)]=\"strategy_TotM.dailyMarketDirectionMaskSummerTotM\" style=\"width: 140px; padding: 2px; border: 1px solid #A0A0A0\" (keypress)=\"OnParameterInputKeypress($event)\" /></td>\r\n                    <td><input type=\"text\" [(ngModel)]=\"strategy_TotM.dailyMarketDirectionMaskSummerTotMM\" style=\"width: 140px; padding: 2px; border: 1px solid #A0A0A0\" (keypress)=\"OnParameterInputKeypress($event)\" /></td>\r\n                </tr>\r\n            </tbody>\r\n        </table>\r\n        <div class=\"SqMenu2\" [hidden]=\"selectedSubStrategyMenuItemId !== 'idMenuItemTotM'\" align=\"left\">\r\n            <ul>\r\n                <li>\r\n                    <span>Masks</span>\r\n                    <ul>\r\n                        <li><span id=\"idMaskBuyHold\" (click)=\"strategy_TotM.MenuItemPresetMasksClicked($event)\">Buy&Hold</span></li>\r\n                        <li><span id=\"idMaskUberVXXOld\" (click)=\"strategy_TotM.MenuItemPresetMasksClicked($event)\">UberVXX old</span></li>\r\n                        <li><span id=\"idMaskBuyUberVXXNew\" (click)=\"strategy_TotM.MenuItemPresetMasksClicked($event)\">UberVXX new</span></li>\r\n                        <li><span id=\"idMaskSPYDerived\" (click)=\"strategy_TotM.MenuItemPresetMasksClicked($event)\">SPY derived</span></li>\r\n                    </ul>\r\n                </li>\r\n            </ul>\r\n        </div>\r\n\r\n        <!--#include virtual=\"/SQQuickTester/Strategies/VIX/VXX_SPY_Controversial.htm\"-->\r\n        <table id=\"inputTable\" [hidden]=\"selectedSubStrategyMenuItemId !== 'idMenuItemVXX_SPY_Controversial'\">\r\n            <colgroup>\r\n                <col class=\"column1\" />\r\n                <col class=\"column2\" />\r\n            </colgroup>\r\n            <thead>\r\n                <tr>\r\n                    <th>SPY Min %Change</th>\r\n                    <th>VXXB Min %Change</th>\r\n                    <th>VXXB Long or Short <br /> after controversial days</th>\r\n                </tr>\r\n            </thead>\r\n            <tbody>\r\n                <tr>\r\n                    <td><input type=\"text\" [(ngModel)]=\"strategy_VXX_SPY_Controversial.spyMinPctMove\" style=\"width: 40px; padding: 2px; border: 1px solid #A0A0A0\" (keypress)=\"OnParameterInputKeypress($event)\" />%</td>\r\n                    <td><input type=\"text\" [(ngModel)]=\"strategy_VXX_SPY_Controversial.vxxMinPctMove\" style=\"width: 40px; padding: 2px; border: 1px solid #A0A0A0\" (keypress)=\"OnParameterInputKeypress($event)\" />%</td>\r\n                    <td>\r\n\r\n                        <select [ngModel]=\"strategy_VXX_SPY_Controversial.selectedVXXLongOrShortTrade\" (ngModelChange)=\"strategy_VXX_SPY_Controversial.vxxLongOrShortTradeChanged($event)\">\r\n                            <option [value]=\"i\" *ngFor=\"let i of strategy_VXX_SPY_Controversial.vxxLongOrShortTrade\">{{i}}</option>\r\n                        </select>\r\n                    </td>\r\n                </tr>\r\n            </tbody>\r\n        </table>\r\n\r\n        <div [hidden]=\"selectedSubStrategyMenuItemId !== 'idMenuItemLETFDiscrRebToNeutral' && selectedSubStrategyMenuItemId !== 'idMenuItemLETFDiscrAddToWinner' && selectedSubStrategyMenuItemId !== 'idMenuItemLETFHarryLong'\">\r\n            <table id=\"inputTable\" align=\"left\">\r\n                <colgroup>\r\n                    <col class=\"column1\" />\r\n                    <col class=\"column2\" />\r\n                </colgroup>\r\n                <thead>\r\n                    <tr>\r\n                        <th>Assets</th>\r\n                        <th style=\"font-size: 70%;\">Assets Weight</th>\r\n                        <th style=\"font-size: 70%;\">Rebalance:'Daily,2d','Weekly<br />,Fridays', 'Monthly,T-1'</th>\r\n                    </tr>\r\n                </thead>\r\n                <tbody>\r\n                    <tr>\r\n                        <td><input type=\"text\" [(ngModel)]=\"strategy_LEtf.assets\" style=\"width: 290px; padding: 2px; border: 1px solid #A0A0A0\" (keypress)=\"OnParameterInputKeypress($event)\" /></td>\r\n                        <td><input type=\"text\" [(ngModel)]=\"strategy_LEtf.assetsConstantWeightPct\" style=\"width: 150px; padding: 2px; border: 1px solid #A0A0A0\" (keypress)=\"OnParameterInputKeypress($event)\" /></td>\r\n                        <td><input type=\"text\" [(ngModel)]=\"strategy_LEtf.rebalancingFrequency\" style=\"width: 150px; padding: 2px; border: 1px solid #A0A0A0\" (keypress)=\"OnParameterInputKeypress($event)\" /></td>\r\n                    </tr>\r\n                </tbody>\r\n            </table>\r\n            <div class=\"SqMenu2\">\r\n                <ul>\r\n                    <li>\r\n                        <span>ParamSets</span>\r\n                        <ul>\r\n                            <li><span id=\"idParamSetHL_-50Comb.SQ_-120H_coctailAgy6\" (click)=\"strategy_LEtf.MenuItemParamSetsClicked($event)\">50%ShortVol,hedges120%**</span></li>\r\n                            <li><span id=\"idParamSetHL_-50Comb.SQ_-100H_coctailAgy6\" (click)=\"strategy_LEtf.MenuItemParamSetsClicked($event)\">50%ShortVol,hedges100%</span></li>\r\n                            <li><span id=\"idParamSetHL_-50Comb.SQ_-80H_coctailAgy6\" (click)=\"strategy_LEtf.MenuItemParamSetsClicked($event)\">50%ShortVol,hedges80%</span></li>\r\n                            <li><span id=\"idParamSetHL_-70VXX.SQ_-75TLT_coctailAgy5\" (click)=\"strategy_LEtf.MenuItemParamSetsClicked($event)\">70VXX.SQ,c.MPT135%B</span></li>\r\n                            <li><span id=\"idParamSetHL_-70VXX.SQ_-75TLT_coctailAgy4\" (click)=\"strategy_LEtf.MenuItemParamSetsClicked($event)\">70VXX.SQ,c.MPT135%A</span></li>\r\n                            <li><span id=\"idParamSetHL_-70VXX.SQ_-75TLT_coctailAgy3\" (click)=\"strategy_LEtf.MenuItemParamSetsClicked($event)\">70VXX.SQ,c.MPT100%*</span></li>\r\n                            <li><span id=\"idParamSetHL_-70VXX.SQ_-75TLT_coctailAgy2\" (click)=\"strategy_LEtf.MenuItemParamSetsClicked($event)\">70VXX.SQ,cocktail Agy2</span></li>\r\n                            <li><span id=\"idParamSetHL_-70VXX.SQ_-75TLT_coctailAgy\" (click)=\"strategy_LEtf.MenuItemParamSetsClicked($event)\">70VXX.SQ,cocktail Agy</span></li>\r\n                            <li><span id=\"idParamSetHL_-70VXX.SQ_-75TLT_coctailDC\" (click)=\"strategy_LEtf.MenuItemParamSetsClicked($event)\">70VXX.SQ,cocktail DC</span></li>\r\n                            <li><span id=\"idParamSetHL_-35TVIX_-25TMV_-28UNG_-8USO_-4JJC\" (click)=\"strategy_LEtf.MenuItemParamSetsClicked($event)\">35TVIX,25TMV,28UNG...*</span></li>\r\n                            <li><span id=\"idParamSetHL_-35TVIX_-65TMV\" (click)=\"strategy_LEtf.MenuItemParamSetsClicked($event)\">35%TVIX - 65%TMV</span></li>\r\n                            <li><span id=\"idParamSetHL_-25TVIX_-75TMV\" (click)=\"strategy_LEtf.MenuItemParamSetsClicked($event)\">HarryLong</span></li>\r\n                            <li><span id=\"idParamSetHL_50VXX_50XIV\" (click)=\"strategy_LEtf.MenuItemParamSetsClicked($event)\">long VXX - long XIV</span></li>\r\n                            <li><span id=\"idParamSetHL_-50VXX.SQ_225TLT\" (click)=\"strategy_LEtf.MenuItemParamSetsClicked($event)\">50%VXX.SQ-225%TLT</span></li>\r\n                            <li><span id=\"idParamSetHL_-50VXX_225TLT\" (click)=\"strategy_LEtf.MenuItemParamSetsClicked($event)\">50%VXX - 225%TLT</span></li>\r\n                            <li><span id=\"idParamSetHL_50XIV_225TLT\" (click)=\"strategy_LEtf.MenuItemParamSetsClicked($event)\">50%XIV - 225%TLT</span></li>\r\n                            <li><span id=\"idParamSetHL_25XIV_112TLT\" (click)=\"strategy_LEtf.MenuItemParamSetsClicked($event)\">25%XIV - 112%TLT</span></li>\r\n                            <li><span id=\"idParamSetHL_-25TVIX_75CASH\" (click)=\"strategy_LEtf.MenuItemParamSetsClicked($event)\">25%TVIX - 75%Cash</span></li>\r\n                            <li><span id=\"idParamSetHL_25CASH_-75TMV\" (click)=\"strategy_LEtf.MenuItemParamSetsClicked($event)\">25%Cash - 75%TMV</span></li>\r\n                            <li><span>*** 50%-50% shorts ***</span></li>\r\n                            <li><span id=\"idParamSetHL_-50URE_-50SRS\" (click)=\"strategy_LEtf.MenuItemParamSetsClicked($event)\">URE - SRS</span></li>\r\n                            <li><span id=\"idParamSetHL_-50DRN_-50DRV\" (click)=\"strategy_LEtf.MenuItemParamSetsClicked($event)\">DRN - DRV</span></li>\r\n                            <li><span id=\"idParamSetHL_-50FAS_-50FAZ\" (click)=\"strategy_LEtf.MenuItemParamSetsClicked($event)\">FAS - FAZ</span></li>\r\n                            <li><span id=\"idParamSetHL_-50VXX_-50XIV\" (click)=\"strategy_LEtf.MenuItemParamSetsClicked($event)\">VXX - XIV</span></li>\r\n                            <li><span id=\"idParamSetHL_-50VXZ_-50ZIV\" (click)=\"strategy_LEtf.MenuItemParamSetsClicked($event)\">VXZB - ZIV</span></li>\r\n                        </ul>\r\n                    </li>\r\n                </ul>\r\n            </div>\r\n\r\n        </div>\r\n\r\n        <div [hidden]=\"selectedSubStrategyMenuItemId !== 'idMenuItemTAA'\">\r\n            <table id=\"inputTable\" align=\"left\">\r\n                <thead>\r\n                    <tr>\r\n                        <th>Assets</th>\r\n                        <th style=\"font-size: 70%;\">Assets<br />Leverage</th>\r\n                        <th style=\"font-size: 70%;\">Rebalance:'Daily,2d','Weekly<br />,Fridays', 'Monthly,T-1'</th>\r\n                        <th style=\"font-size: 70%;\">%Channel<br /> Lookback Days</th>\r\n                        <th style=\"font-size: 70%;\">%Channel<br /> %Limits</th>\r\n                        <th style=\"font-size: 70%;\">%Channel<br /> IsActiveEveryDay</th>\r\n                        <th style=\"font-size: 70%;\">%Channel<br /> IsConditional</th>\r\n                    </tr>\r\n                </thead>\r\n                <tbody>\r\n                    <tr>\r\n                        <td><input type=\"text\" [(ngModel)]=\"strategy_AssetAllocation.assets\" style=\"width: 250px; padding: 2px; border: 1px solid #A0A0A0\" (keypress)=\"OnParameterInputKeypress($event)\" /></td>\r\n                        <td><input type=\"text\" [(ngModel)]=\"strategy_AssetAllocation.assetsConstantLeverage\" style=\"width: 60px; padding: 2px; border: 1px solid #A0A0A0\" (keypress)=\"OnParameterInputKeypress($event)\" /></td>\r\n                        <td><input type=\"text\" [(ngModel)]=\"strategy_AssetAllocation.rebalancingFrequency\" style=\"width: 150px; padding: 2px; border: 1px solid #A0A0A0\" (keypress)=\"OnParameterInputKeypress($event)\" /></td>\r\n                        <td><input type=\"text\" [(ngModel)]=\"strategy_AssetAllocation.pctChannelLookbackDays\" style=\"width: 100px; padding: 2px; border: 1px solid #A0A0A0\" (keypress)=\"OnParameterInputKeypress($event)\" /></td>\r\n                        <td><input type=\"text\" [(ngModel)]=\"strategy_AssetAllocation.pctChannelPctLimits\" style=\"width: 50px; padding: 2px; border: 1px solid #A0A0A0\" (keypress)=\"OnParameterInputKeypress($event)\" />%</td>\r\n                        <td><input type=\"text\" [(ngModel)]=\"strategy_AssetAllocation.isPctChannelActiveEveryDay\" style=\"width: 30px; padding: 2px; border: 1px solid #A0A0A0\" (keypress)=\"OnParameterInputKeypress($event)\" /></td>\r\n                        <td><input type=\"text\" [(ngModel)]=\"strategy_AssetAllocation.isPctChannelConditional\" style=\"width: 30px; padding: 2px; border: 1px solid #A0A0A0\" (keypress)=\"OnParameterInputKeypress($event)\" /></td>\r\n                    </tr>\r\n                </tbody>\r\n            </table>\r\n            <table id=\"inputTable\" align=\"left\">\r\n                <thead>\r\n                    <tr>\r\n                        <th style=\"font-size: 70%;\">HV Lookback<br /> Days</th>\r\n                        <th style=\"font-size: 70%;\">Is Cash Allocated<br /> for Non-actives</th>\r\n                        <th style=\"font-size: 70%;\">Cash Equivalent<br /> Ticker</th>\r\n                        <th style=\"font-size: 70%;\">CLMT<br /> Leverage </th>\r\n                        <th style=\"font-size: 70%;\">UberVXX<br /> Events</th>\r\n                        <th style=\"font-size: 70%;\">Debug Detail<br /> To Html</th>\r\n                    </tr>\r\n                </thead>\r\n                <tbody>\r\n                    <tr>\r\n                        <td><input type=\"text\" [(ngModel)]=\"strategy_AssetAllocation.histVolLookbackDays\" style=\"width: 30px; padding: 2px; border: 1px solid #A0A0A0\" (keypress)=\"OnParameterInputKeypress($event)\" /></td>\r\n                        <td><input type=\"text\" [(ngModel)]=\"strategy_AssetAllocation.isCashAllocatedForNonActives\" style=\"width: 30px; padding: 2px; border: 1px solid #A0A0A0\" (keypress)=\"OnParameterInputKeypress($event)\" /></td>\r\n                        <td><input type=\"text\" [(ngModel)]=\"strategy_AssetAllocation.cashEquivalentTicker\" style=\"width: 30px; padding: 2px; border: 1px solid #A0A0A0\" (keypress)=\"OnParameterInputKeypress($event)\" /></td>\r\n                        <td><input type=\"text\" [(ngModel)]=\"strategy_AssetAllocation.dynamicLeverageClmtParams\" style=\"width: 100px; padding: 2px; border: 1px solid #A0A0A0\" (keypress)=\"OnParameterInputKeypress($event)\" /></td>\r\n                        <td><input type=\"text\" [(ngModel)]=\"strategy_AssetAllocation.uberVxxEventsParams\" style=\"width: 100px; padding: 2px; border: 1px solid #A0A0A0\" (keypress)=\"OnParameterInputKeypress($event)\" /></td>\r\n                        <td><input type=\"text\" [(ngModel)]=\"strategy_AssetAllocation.debugDetailToHtml\" style=\"width: 200px; padding: 2px; border: 1px solid #A0A0A0\" (keypress)=\"OnParameterInputKeypress($event)\" /></td>\r\n                    </tr>\r\n                </tbody>\r\n            </table>\r\n            <div class=\"SqMenu2\">\r\n                <ul>\r\n                    <li>\r\n                        <span>ParamSets T</span>\r\n                        <ul>\r\n                            <li><span id=\"idParamSetTAA_GlobVnqIbb\" (click)=\"strategy_AssetAllocation.MenuItemParamSetsClicked($event)\">Global+VNQ+BIB Live</span></li>\r\n                            <li><span id=\"idParamSetTAA_Glob_Live\" (click)=\"strategy_AssetAllocation.MenuItemParamSetsClicked($event)\">GlobalAssets Live</span></li>\r\n                            <li><span id=\"idParamSetTAA_GC_Live\" (click)=\"strategy_AssetAllocation.MenuItemParamSetsClicked($event)\">GameChangers Live</span></li>\r\n                            <li><span id=\"idParamSetTAA_VaradiOriginal\" (click)=\"strategy_AssetAllocation.MenuItemParamSetsClicked($event)\">Varadi's original</span></li>\r\n                        </ul>\r\n                    </li>\r\n                </ul>para\r\n            </div>\r\n        </div>\r\n\r\n        <!--// How to Eliminate Whitespace around Server Side Includes when Pages are UTF-8 Encoded? http://stackoverflow.com/questions/7196993/how-to-eliminate-whitespace-around-server-side-includes-when-pages-are-utf-8-enc-->\r\n        <!--<div style=\"width: 50%; height: 1px; background: #ffffff; overflow: hidden;\"></div>--> <!--extra 1 pixel line, so that the input rows doesn't touch each other -->\r\n        <div style=\"width: 100%; height: 2px; background: #ffffff; overflow: hidden;\"></div>\r\n        <div style=\"width: 100%; height: 1px; background: #808080; overflow: hidden;\"></div>\r\n        <!--Ideas what to put here as Statistics come from Portfolio123 http://imarketsignals.com/2015/best8-sp500-min-volatility-large-cap-portfolio-management-system/-->\r\n\r\n        <table id=\"statisticsTable\">\r\n            <colgroup>\r\n                <col class=\"column1\" />\r\n                <col class=\"column2\" />\r\n                <col class=\"column3\" />\r\n                <col class=\"column3\" />\r\n            </colgroup>\r\n            <thead>\r\n            </thead>\r\n            <tbody>\r\n                <tr class='even'>\r\n                    <td id=\"idTdGeneral\">General:</td>\r\n                    <td>Start Date: <span> {{startDateStr}} </span></td>\r\n                    <td id=\"idTdRebalanceFrequency\">Rebalance Frequency: <span> {{rebalanceFrequencyStr}} </span></td>\r\n                    <td>Benchmark: <span> {{benchmarkStr}} </span></td>\r\n                </tr>\r\n            </tbody>\r\n        </table>\r\n        <span id=\"idSpanStatistics\">Statistics (as of {{endDateStr}}):</span>\r\n        <br>\r\n\r\n\r\n        <table id=\"statisticsTable\">\r\n            <colgroup>\r\n                <col class=\"column1\" />\r\n                <col class=\"column2\" />\r\n                <col class=\"column3\" />\r\n            </colgroup>\r\n            <thead>\r\n            </thead>\r\n            <tbody>\r\n                <tr class='even'>\r\n                    <td>PV Start Value: <span> &#36;{{pvStartValue | number:'.0-2'}} </span></td>\r\n                    <!--<td class='green' onclick=\"GlobalScopeInvertVisibilityOfTableRow('idWinter__TotMT-17')\">0.008%<span style=\"color: #2581cc; font-size: x-small; vertical-align:super;\">i</span></td>-->\r\n                    <td>PV Final Value: <span> &#36;{{pvEndValue | number:'.0-2'}} </span></td>\r\n                    <td>Total Return: <span> {{100*totalGainPct | number: '.0-2'}}&#37; </span></td>\r\n                </tr>\r\n                <tr>\r\n                    <td id=\"tdCagr\">CAGR: <span> {{100*cagr | number:'.0-2'}}% </span></td>\r\n                    <td>Annualized StDev: <span> {{100*annualizedStDev | number:'.0-2'}}% </span></td>\r\n                    <td id=\"tdSharpe\">Sharpe: <span> {{sharpeRatio | number:'.0-2'}} </span></td>\r\n                </tr>\r\n                <tr class='even'>\r\n                    <td>Max Drawdown: <span> {{100*maxDD | number:'.0-2'}}% </span></td>\r\n                    <td><a target=\"_blank\" href=\"http://en.wikipedia.org/wiki/Ulcer_index\">Ulcer</a> (Vol=qMean DD): <span> {{100*ulcerInd | number:'.0-2'}}% </span></td>\r\n                    <td>Max.TradingDays in Drawdown: <span> {{maxTradingDaysInDD}} </span></td>\r\n                    <td></td>\r\n                </tr>\r\n                <tr>\r\n                    <td>Winners: <span> {{winnersStr}} </span></td>\r\n                    <td>Losers: <span> {{losersStr}} </span></td>\r\n                    <td>Annualized <a target=\"_blank\" href=\"http://www.redrockcapital.com/Sortino__A__Sharper__Ratio_Red_Rock_Capital.pdf\">Sortino</a>: <span> {{sortinoRatio | number:'.0-2'}} </span></td>\r\n                    <td></td>\r\n                </tr>\r\n                <tr class='even'>\r\n                    <td>Benchmark CAGR: <span> {{100*benchmarkCagr | number:'.0-2'}}% </span></td>\r\n                    <td>Benchmark Max Drawdown: <span> {{100*benchmarkMaxDD | number:'.0-2'}}% </span></td>\r\n                    <td>Correlation with Benchmark: <span> {{benchmarkCorrelation | number:'.0-2'}}</span></td>\r\n                </tr>\r\n            </tbody>\r\n        </table>\r\n\r\n\r\n        <div style=\"width: 100%; height: 1px; background: #808080; overflow: hidden;\"></div>\r\n        <div id=\"tv_chart_container\"></div>\r\n\r\n        <span id=\"idAlwaysVisibleSwitch1\" class=\"alwaysVisibleSwitchClass\" (click)=\"SQToggle('idNoteFromStrategyDiv', 'idAlwaysVisibleSwitch1', 'note from strategy')\">+ Show General Parameters</span>\r\n\r\n        <br />\r\n        <div id=\"idNoteFromStrategyDiv\" style=\"display: block;\">\r\n            <!--<span ng-bind-html=\"htmlNoteFromStrategy\"> This needs angular-sanitize.js, if we need recursive Angular {{}}, but we don't need it. Better to stick with pure HTML5  </span>-->\r\n            <span id=\"idHtmlNoteFromStrategy\">  </span>\r\n        </div>\r\n\r\n\r\n\r\n\r\n        <br>\r\n        <span id=\"idSpanStatistics\">Holdings (as of {{endDateStr}}):</span>\r\n        <br>\r\n        <table id=\"statisticsTable\">\r\n            <colgroup>\r\n                <col class=\"column1\" />\r\n                <col class=\"column2\" />\r\n                <col class=\"column3\" />\r\n            </colgroup>\r\n            <thead>\r\n            </thead>\r\n            <tbody>\r\n                <tr class='even'>\r\n                    <td>Cash: <span> ${{pvCash | number:'.0-2'}} </span></td>\r\n                    <td>Number of positions: <span> {{nPositions}} </span></td>\r\n                </tr>\r\n            </tbody>\r\n        </table>\r\n\r\n\r\n        Holdings List:\r\n        <span> {{holdingsListStr}} </span>\r\n        <br>\r\n        <br>\r\n        ErrorMessage:\r\n        <span> {{errorMessage}} </span>\r\n        <br>\r\n        DebugMessage:\r\n        <span> {{debugMessage}} </span>\r\n        <br>\r\n        Profiling:\r\n        <span>JS BacktestCallback: {{profilingBacktestCallbackMSec | number}}ms, BacktestAtChartReadyStart(chart removed/created): {{profilingBacktestAtChartReadyStartMSec | number}}ms, BacktestAtChartReadyEnd(TimeFrame clicked): {{profilingBacktestAtChartReadyEndMSec | number}}ms </span>\r\n        <br>\r\n\r\n        <span id=\"idAlwaysVisibleSwitchDebug\" class=\"alwaysVisibleSwitchClass\" (click)=\"SQToggle('idDebugInfoDiv', 'idAlwaysVisibleSwitchDebug', 'debug info')\">+ Show debug info</span>\r\n\r\n        <br />\r\n        <div id=\"idDebugInfoDiv\" style=\"display: none;\">\r\n            <!--<span ng-bind-html=\"htmlNoteFromStrategy\"> This needs angular-sanitize.js, if we need recursive Angular {{}}, but we don't need it. Better to stick with pure HTML5  </span>-->\r\n            <span id=\"idDebugInfoSpan\">\r\n                <br>ChartDataInStr:\r\n                <pre>{{chartDataInStr}} </pre>\r\n                <br>\r\n            </span>\r\n        </div>\r\n\r\n\r\n        <br>\r\n        <!--<span>{{tipToUser}}</span>-->\r\n        <!--<br />\r\n    <button class=\"mymyButton1Class\" (click)=\"onClickMe()\">Click me!</button>\r\n    <br />\r\n    <br />{{clickMessage }}-->\r\n\r\n\r\n    </div>\r\n    </div>\r\n";

/***/ }),
/* 40 */
/***/ (function(module, exports, __webpack_require__) {

"use strict";
// Copyright Joyent, Inc. and other Node contributors.
//
// Permission is hereby granted, free of charge, to any person obtaining a
// copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to permit
// persons to whom the Software is furnished to do so, subject to the
// following conditions:
//
// The above copyright notice and this permission notice shall be included
// in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS
// OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN
// NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM,
// DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE
// USE OR OTHER DEALINGS IN THE SOFTWARE.



// If obj.hasOwnProperty has been overridden, then calling
// obj.hasOwnProperty(prop) will break.
// See: https://github.com/joyent/node/issues/1707
function hasOwnProperty(obj, prop) {
  return Object.prototype.hasOwnProperty.call(obj, prop);
}

module.exports = function(qs, sep, eq, options) {
  sep = sep || '&';
  eq = eq || '=';
  var obj = {};

  if (typeof qs !== 'string' || qs.length === 0) {
    return obj;
  }

  var regexp = /\+/g;
  qs = qs.split(sep);

  var maxKeys = 1000;
  if (options && typeof options.maxKeys === 'number') {
    maxKeys = options.maxKeys;
  }

  var len = qs.length;
  // maxKeys <= 0 means that we should not limit keys count
  if (maxKeys > 0 && len > maxKeys) {
    len = maxKeys;
  }

  for (var i = 0; i < len; ++i) {
    var x = qs[i].replace(regexp, '%20'),
        idx = x.indexOf(eq),
        kstr, vstr, k, v;

    if (idx >= 0) {
      kstr = x.substr(0, idx);
      vstr = x.substr(idx + 1);
    } else {
      kstr = x;
      vstr = '';
    }

    k = decodeURIComponent(kstr);
    v = decodeURIComponent(vstr);

    if (!hasOwnProperty(obj, k)) {
      obj[k] = v;
    } else if (isArray(obj[k])) {
      obj[k].push(v);
    } else {
      obj[k] = [obj[k], v];
    }
  }

  return obj;
};

var isArray = Array.isArray || function (xs) {
  return Object.prototype.toString.call(xs) === '[object Array]';
};


/***/ }),
/* 41 */
/***/ (function(module, exports, __webpack_require__) {

"use strict";
// Copyright Joyent, Inc. and other Node contributors.
//
// Permission is hereby granted, free of charge, to any person obtaining a
// copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to permit
// persons to whom the Software is furnished to do so, subject to the
// following conditions:
//
// The above copyright notice and this permission notice shall be included
// in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS
// OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN
// NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM,
// DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE
// USE OR OTHER DEALINGS IN THE SOFTWARE.



var stringifyPrimitive = function(v) {
  switch (typeof v) {
    case 'string':
      return v;

    case 'boolean':
      return v ? 'true' : 'false';

    case 'number':
      return isFinite(v) ? v : '';

    default:
      return '';
  }
};

module.exports = function(obj, sep, eq, name) {
  sep = sep || '&';
  eq = eq || '=';
  if (obj === null) {
    obj = undefined;
  }

  if (typeof obj === 'object') {
    return map(objectKeys(obj), function(k) {
      var ks = encodeURIComponent(stringifyPrimitive(k)) + eq;
      if (isArray(obj[k])) {
        return map(obj[k], function(v) {
          return ks + encodeURIComponent(stringifyPrimitive(v));
        }).join(sep);
      } else {
        return ks + encodeURIComponent(stringifyPrimitive(obj[k]));
      }
    }).join(sep);

  }

  if (!name) return '';
  return encodeURIComponent(stringifyPrimitive(name)) + eq +
         encodeURIComponent(stringifyPrimitive(obj));
};

var isArray = Array.isArray || function (xs) {
  return Object.prototype.toString.call(xs) === '[object Array]';
};

function map (xs, f) {
  if (xs.map) return xs.map(f);
  var res = [];
  for (var i = 0; i < xs.length; i++) {
    res.push(f(xs[i], i));
  }
  return res;
}

var objectKeys = Object.keys || function (obj) {
  var res = [];
  for (var key in obj) {
    if (Object.prototype.hasOwnProperty.call(obj, key)) res.push(key);
  }
  return res;
};


/***/ }),
/* 42 */
/***/ (function(module, exports, __webpack_require__) {

"use strict";


exports.decode = exports.parse = __webpack_require__(40);
exports.encode = exports.stringify = __webpack_require__(41);


/***/ }),
/* 43 */
/***/ (function(module, exports, __webpack_require__) {

/* WEBPACK VAR INJECTION */(function(process, global) {/*! *****************************************************************************
Copyright (C) Microsoft. All rights reserved.
Licensed under the Apache License, Version 2.0 (the "License"); you may not use
this file except in compliance with the License. You may obtain a copy of the
License at http://www.apache.org/licenses/LICENSE-2.0

THIS CODE IS PROVIDED ON AN *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED
WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE,
MERCHANTABLITY OR NON-INFRINGEMENT.

See the Apache Version 2.0 License for specific language governing permissions
and limitations under the License.
***************************************************************************** */
var Reflect;
(function (Reflect) {
    "use strict";
    var hasOwn = Object.prototype.hasOwnProperty;
    // feature test for Symbol support
    var supportsSymbol = typeof Symbol === "function";
    var toPrimitiveSymbol = supportsSymbol && typeof Symbol.toPrimitive !== "undefined" ? Symbol.toPrimitive : "@@toPrimitive";
    var iteratorSymbol = supportsSymbol && typeof Symbol.iterator !== "undefined" ? Symbol.iterator : "@@iterator";
    var HashMap;
    (function (HashMap) {
        var supportsCreate = typeof Object.create === "function"; // feature test for Object.create support
        var supportsProto = { __proto__: [] } instanceof Array; // feature test for __proto__ support
        var downLevel = !supportsCreate && !supportsProto;
        // create an object in dictionary mode (a.k.a. "slow" mode in v8)
        HashMap.create = supportsCreate
            ? function () { return MakeDictionary(Object.create(null)); }
            : supportsProto
                ? function () { return MakeDictionary({ __proto__: null }); }
                : function () { return MakeDictionary({}); };
        HashMap.has = downLevel
            ? function (map, key) { return hasOwn.call(map, key); }
            : function (map, key) { return key in map; };
        HashMap.get = downLevel
            ? function (map, key) { return hasOwn.call(map, key) ? map[key] : undefined; }
            : function (map, key) { return map[key]; };
    })(HashMap || (HashMap = {}));
    // Load global or shim versions of Map, Set, and WeakMap
    var functionPrototype = Object.getPrototypeOf(Function);
    var usePolyfill = typeof process === "object" && process.env && process.env["REFLECT_METADATA_USE_MAP_POLYFILL"] === "true";
    var _Map = !usePolyfill && typeof Map === "function" && typeof Map.prototype.entries === "function" ? Map : CreateMapPolyfill();
    var _Set = !usePolyfill && typeof Set === "function" && typeof Set.prototype.entries === "function" ? Set : CreateSetPolyfill();
    var _WeakMap = !usePolyfill && typeof WeakMap === "function" ? WeakMap : CreateWeakMapPolyfill();
    // [[Metadata]] internal slot
    // https://rbuckton.github.io/reflect-metadata/#ordinary-object-internal-methods-and-internal-slots
    var Metadata = new _WeakMap();
    /**
      * Applies a set of decorators to a property of a target object.
      * @param decorators An array of decorators.
      * @param target The target object.
      * @param propertyKey (Optional) The property key to decorate.
      * @param attributes (Optional) The property descriptor for the target key.
      * @remarks Decorators are applied in reverse order.
      * @example
      *
      *     class Example {
      *         // property declarations are not part of ES6, though they are valid in TypeScript:
      *         // static staticProperty;
      *         // property;
      *
      *         constructor(p) { }
      *         static staticMethod(p) { }
      *         method(p) { }
      *     }
      *
      *     // constructor
      *     Example = Reflect.decorate(decoratorsArray, Example);
      *
      *     // property (on constructor)
      *     Reflect.decorate(decoratorsArray, Example, "staticProperty");
      *
      *     // property (on prototype)
      *     Reflect.decorate(decoratorsArray, Example.prototype, "property");
      *
      *     // method (on constructor)
      *     Object.defineProperty(Example, "staticMethod",
      *         Reflect.decorate(decoratorsArray, Example, "staticMethod",
      *             Object.getOwnPropertyDescriptor(Example, "staticMethod")));
      *
      *     // method (on prototype)
      *     Object.defineProperty(Example.prototype, "method",
      *         Reflect.decorate(decoratorsArray, Example.prototype, "method",
      *             Object.getOwnPropertyDescriptor(Example.prototype, "method")));
      *
      */
    function decorate(decorators, target, propertyKey, attributes) {
        if (!IsUndefined(propertyKey)) {
            if (!IsArray(decorators))
                throw new TypeError();
            if (!IsObject(target))
                throw new TypeError();
            if (!IsObject(attributes) && !IsUndefined(attributes) && !IsNull(attributes))
                throw new TypeError();
            if (IsNull(attributes))
                attributes = undefined;
            propertyKey = ToPropertyKey(propertyKey);
            return DecorateProperty(decorators, target, propertyKey, attributes);
        }
        else {
            if (!IsArray(decorators))
                throw new TypeError();
            if (!IsConstructor(target))
                throw new TypeError();
            return DecorateConstructor(decorators, target);
        }
    }
    Reflect.decorate = decorate;
    // 4.1.2 Reflect.metadata(metadataKey, metadataValue)
    // https://rbuckton.github.io/reflect-metadata/#reflect.metadata
    /**
      * A default metadata decorator factory that can be used on a class, class member, or parameter.
      * @param metadataKey The key for the metadata entry.
      * @param metadataValue The value for the metadata entry.
      * @returns A decorator function.
      * @remarks
      * If `metadataKey` is already defined for the target and target key, the
      * metadataValue for that key will be overwritten.
      * @example
      *
      *     // constructor
      *     @Reflect.metadata(key, value)
      *     class Example {
      *     }
      *
      *     // property (on constructor, TypeScript only)
      *     class Example {
      *         @Reflect.metadata(key, value)
      *         static staticProperty;
      *     }
      *
      *     // property (on prototype, TypeScript only)
      *     class Example {
      *         @Reflect.metadata(key, value)
      *         property;
      *     }
      *
      *     // method (on constructor)
      *     class Example {
      *         @Reflect.metadata(key, value)
      *         static staticMethod() { }
      *     }
      *
      *     // method (on prototype)
      *     class Example {
      *         @Reflect.metadata(key, value)
      *         method() { }
      *     }
      *
      */
    function metadata(metadataKey, metadataValue) {
        function decorator(target, propertyKey) {
            if (!IsObject(target))
                throw new TypeError();
            if (!IsUndefined(propertyKey) && !IsPropertyKey(propertyKey))
                throw new TypeError();
            OrdinaryDefineOwnMetadata(metadataKey, metadataValue, target, propertyKey);
        }
        return decorator;
    }
    Reflect.metadata = metadata;
    /**
      * Define a unique metadata entry on the target.
      * @param metadataKey A key used to store and retrieve metadata.
      * @param metadataValue A value that contains attached metadata.
      * @param target The target object on which to define metadata.
      * @param propertyKey (Optional) The property key for the target.
      * @example
      *
      *     class Example {
      *         // property declarations are not part of ES6, though they are valid in TypeScript:
      *         // static staticProperty;
      *         // property;
      *
      *         constructor(p) { }
      *         static staticMethod(p) { }
      *         method(p) { }
      *     }
      *
      *     // constructor
      *     Reflect.defineMetadata("custom:annotation", options, Example);
      *
      *     // property (on constructor)
      *     Reflect.defineMetadata("custom:annotation", options, Example, "staticProperty");
      *
      *     // property (on prototype)
      *     Reflect.defineMetadata("custom:annotation", options, Example.prototype, "property");
      *
      *     // method (on constructor)
      *     Reflect.defineMetadata("custom:annotation", options, Example, "staticMethod");
      *
      *     // method (on prototype)
      *     Reflect.defineMetadata("custom:annotation", options, Example.prototype, "method");
      *
      *     // decorator factory as metadata-producing annotation.
      *     function MyAnnotation(options): Decorator {
      *         return (target, key?) => Reflect.defineMetadata("custom:annotation", options, target, key);
      *     }
      *
      */
    function defineMetadata(metadataKey, metadataValue, target, propertyKey) {
        if (!IsObject(target))
            throw new TypeError();
        if (!IsUndefined(propertyKey))
            propertyKey = ToPropertyKey(propertyKey);
        return OrdinaryDefineOwnMetadata(metadataKey, metadataValue, target, propertyKey);
    }
    Reflect.defineMetadata = defineMetadata;
    /**
      * Gets a value indicating whether the target object or its prototype chain has the provided metadata key defined.
      * @param metadataKey A key used to store and retrieve metadata.
      * @param target The target object on which the metadata is defined.
      * @param propertyKey (Optional) The property key for the target.
      * @returns `true` if the metadata key was defined on the target object or its prototype chain; otherwise, `false`.
      * @example
      *
      *     class Example {
      *         // property declarations are not part of ES6, though they are valid in TypeScript:
      *         // static staticProperty;
      *         // property;
      *
      *         constructor(p) { }
      *         static staticMethod(p) { }
      *         method(p) { }
      *     }
      *
      *     // constructor
      *     result = Reflect.hasMetadata("custom:annotation", Example);
      *
      *     // property (on constructor)
      *     result = Reflect.hasMetadata("custom:annotation", Example, "staticProperty");
      *
      *     // property (on prototype)
      *     result = Reflect.hasMetadata("custom:annotation", Example.prototype, "property");
      *
      *     // method (on constructor)
      *     result = Reflect.hasMetadata("custom:annotation", Example, "staticMethod");
      *
      *     // method (on prototype)
      *     result = Reflect.hasMetadata("custom:annotation", Example.prototype, "method");
      *
      */
    function hasMetadata(metadataKey, target, propertyKey) {
        if (!IsObject(target))
            throw new TypeError();
        if (!IsUndefined(propertyKey))
            propertyKey = ToPropertyKey(propertyKey);
        return OrdinaryHasMetadata(metadataKey, target, propertyKey);
    }
    Reflect.hasMetadata = hasMetadata;
    /**
      * Gets a value indicating whether the target object has the provided metadata key defined.
      * @param metadataKey A key used to store and retrieve metadata.
      * @param target The target object on which the metadata is defined.
      * @param propertyKey (Optional) The property key for the target.
      * @returns `true` if the metadata key was defined on the target object; otherwise, `false`.
      * @example
      *
      *     class Example {
      *         // property declarations are not part of ES6, though they are valid in TypeScript:
      *         // static staticProperty;
      *         // property;
      *
      *         constructor(p) { }
      *         static staticMethod(p) { }
      *         method(p) { }
      *     }
      *
      *     // constructor
      *     result = Reflect.hasOwnMetadata("custom:annotation", Example);
      *
      *     // property (on constructor)
      *     result = Reflect.hasOwnMetadata("custom:annotation", Example, "staticProperty");
      *
      *     // property (on prototype)
      *     result = Reflect.hasOwnMetadata("custom:annotation", Example.prototype, "property");
      *
      *     // method (on constructor)
      *     result = Reflect.hasOwnMetadata("custom:annotation", Example, "staticMethod");
      *
      *     // method (on prototype)
      *     result = Reflect.hasOwnMetadata("custom:annotation", Example.prototype, "method");
      *
      */
    function hasOwnMetadata(metadataKey, target, propertyKey) {
        if (!IsObject(target))
            throw new TypeError();
        if (!IsUndefined(propertyKey))
            propertyKey = ToPropertyKey(propertyKey);
        return OrdinaryHasOwnMetadata(metadataKey, target, propertyKey);
    }
    Reflect.hasOwnMetadata = hasOwnMetadata;
    /**
      * Gets the metadata value for the provided metadata key on the target object or its prototype chain.
      * @param metadataKey A key used to store and retrieve metadata.
      * @param target The target object on which the metadata is defined.
      * @param propertyKey (Optional) The property key for the target.
      * @returns The metadata value for the metadata key if found; otherwise, `undefined`.
      * @example
      *
      *     class Example {
      *         // property declarations are not part of ES6, though they are valid in TypeScript:
      *         // static staticProperty;
      *         // property;
      *
      *         constructor(p) { }
      *         static staticMethod(p) { }
      *         method(p) { }
      *     }
      *
      *     // constructor
      *     result = Reflect.getMetadata("custom:annotation", Example);
      *
      *     // property (on constructor)
      *     result = Reflect.getMetadata("custom:annotation", Example, "staticProperty");
      *
      *     // property (on prototype)
      *     result = Reflect.getMetadata("custom:annotation", Example.prototype, "property");
      *
      *     // method (on constructor)
      *     result = Reflect.getMetadata("custom:annotation", Example, "staticMethod");
      *
      *     // method (on prototype)
      *     result = Reflect.getMetadata("custom:annotation", Example.prototype, "method");
      *
      */
    function getMetadata(metadataKey, target, propertyKey) {
        if (!IsObject(target))
            throw new TypeError();
        if (!IsUndefined(propertyKey))
            propertyKey = ToPropertyKey(propertyKey);
        return OrdinaryGetMetadata(metadataKey, target, propertyKey);
    }
    Reflect.getMetadata = getMetadata;
    /**
      * Gets the metadata value for the provided metadata key on the target object.
      * @param metadataKey A key used to store and retrieve metadata.
      * @param target The target object on which the metadata is defined.
      * @param propertyKey (Optional) The property key for the target.
      * @returns The metadata value for the metadata key if found; otherwise, `undefined`.
      * @example
      *
      *     class Example {
      *         // property declarations are not part of ES6, though they are valid in TypeScript:
      *         // static staticProperty;
      *         // property;
      *
      *         constructor(p) { }
      *         static staticMethod(p) { }
      *         method(p) { }
      *     }
      *
      *     // constructor
      *     result = Reflect.getOwnMetadata("custom:annotation", Example);
      *
      *     // property (on constructor)
      *     result = Reflect.getOwnMetadata("custom:annotation", Example, "staticProperty");
      *
      *     // property (on prototype)
      *     result = Reflect.getOwnMetadata("custom:annotation", Example.prototype, "property");
      *
      *     // method (on constructor)
      *     result = Reflect.getOwnMetadata("custom:annotation", Example, "staticMethod");
      *
      *     // method (on prototype)
      *     result = Reflect.getOwnMetadata("custom:annotation", Example.prototype, "method");
      *
      */
    function getOwnMetadata(metadataKey, target, propertyKey) {
        if (!IsObject(target))
            throw new TypeError();
        if (!IsUndefined(propertyKey))
            propertyKey = ToPropertyKey(propertyKey);
        return OrdinaryGetOwnMetadata(metadataKey, target, propertyKey);
    }
    Reflect.getOwnMetadata = getOwnMetadata;
    /**
      * Gets the metadata keys defined on the target object or its prototype chain.
      * @param target The target object on which the metadata is defined.
      * @param propertyKey (Optional) The property key for the target.
      * @returns An array of unique metadata keys.
      * @example
      *
      *     class Example {
      *         // property declarations are not part of ES6, though they are valid in TypeScript:
      *         // static staticProperty;
      *         // property;
      *
      *         constructor(p) { }
      *         static staticMethod(p) { }
      *         method(p) { }
      *     }
      *
      *     // constructor
      *     result = Reflect.getMetadataKeys(Example);
      *
      *     // property (on constructor)
      *     result = Reflect.getMetadataKeys(Example, "staticProperty");
      *
      *     // property (on prototype)
      *     result = Reflect.getMetadataKeys(Example.prototype, "property");
      *
      *     // method (on constructor)
      *     result = Reflect.getMetadataKeys(Example, "staticMethod");
      *
      *     // method (on prototype)
      *     result = Reflect.getMetadataKeys(Example.prototype, "method");
      *
      */
    function getMetadataKeys(target, propertyKey) {
        if (!IsObject(target))
            throw new TypeError();
        if (!IsUndefined(propertyKey))
            propertyKey = ToPropertyKey(propertyKey);
        return OrdinaryMetadataKeys(target, propertyKey);
    }
    Reflect.getMetadataKeys = getMetadataKeys;
    /**
      * Gets the unique metadata keys defined on the target object.
      * @param target The target object on which the metadata is defined.
      * @param propertyKey (Optional) The property key for the target.
      * @returns An array of unique metadata keys.
      * @example
      *
      *     class Example {
      *         // property declarations are not part of ES6, though they are valid in TypeScript:
      *         // static staticProperty;
      *         // property;
      *
      *         constructor(p) { }
      *         static staticMethod(p) { }
      *         method(p) { }
      *     }
      *
      *     // constructor
      *     result = Reflect.getOwnMetadataKeys(Example);
      *
      *     // property (on constructor)
      *     result = Reflect.getOwnMetadataKeys(Example, "staticProperty");
      *
      *     // property (on prototype)
      *     result = Reflect.getOwnMetadataKeys(Example.prototype, "property");
      *
      *     // method (on constructor)
      *     result = Reflect.getOwnMetadataKeys(Example, "staticMethod");
      *
      *     // method (on prototype)
      *     result = Reflect.getOwnMetadataKeys(Example.prototype, "method");
      *
      */
    function getOwnMetadataKeys(target, propertyKey) {
        if (!IsObject(target))
            throw new TypeError();
        if (!IsUndefined(propertyKey))
            propertyKey = ToPropertyKey(propertyKey);
        return OrdinaryOwnMetadataKeys(target, propertyKey);
    }
    Reflect.getOwnMetadataKeys = getOwnMetadataKeys;
    /**
      * Deletes the metadata entry from the target object with the provided key.
      * @param metadataKey A key used to store and retrieve metadata.
      * @param target The target object on which the metadata is defined.
      * @param propertyKey (Optional) The property key for the target.
      * @returns `true` if the metadata entry was found and deleted; otherwise, false.
      * @example
      *
      *     class Example {
      *         // property declarations are not part of ES6, though they are valid in TypeScript:
      *         // static staticProperty;
      *         // property;
      *
      *         constructor(p) { }
      *         static staticMethod(p) { }
      *         method(p) { }
      *     }
      *
      *     // constructor
      *     result = Reflect.deleteMetadata("custom:annotation", Example);
      *
      *     // property (on constructor)
      *     result = Reflect.deleteMetadata("custom:annotation", Example, "staticProperty");
      *
      *     // property (on prototype)
      *     result = Reflect.deleteMetadata("custom:annotation", Example.prototype, "property");
      *
      *     // method (on constructor)
      *     result = Reflect.deleteMetadata("custom:annotation", Example, "staticMethod");
      *
      *     // method (on prototype)
      *     result = Reflect.deleteMetadata("custom:annotation", Example.prototype, "method");
      *
      */
    function deleteMetadata(metadataKey, target, propertyKey) {
        if (!IsObject(target))
            throw new TypeError();
        if (!IsUndefined(propertyKey))
            propertyKey = ToPropertyKey(propertyKey);
        var metadataMap = GetOrCreateMetadataMap(target, propertyKey, /*Create*/ false);
        if (IsUndefined(metadataMap))
            return false;
        if (!metadataMap.delete(metadataKey))
            return false;
        if (metadataMap.size > 0)
            return true;
        var targetMetadata = Metadata.get(target);
        targetMetadata.delete(propertyKey);
        if (targetMetadata.size > 0)
            return true;
        Metadata.delete(target);
        return true;
    }
    Reflect.deleteMetadata = deleteMetadata;
    function DecorateConstructor(decorators, target) {
        for (var i = decorators.length - 1; i >= 0; --i) {
            var decorator = decorators[i];
            var decorated = decorator(target);
            if (!IsUndefined(decorated) && !IsNull(decorated)) {
                if (!IsConstructor(decorated))
                    throw new TypeError();
                target = decorated;
            }
        }
        return target;
    }
    function DecorateProperty(decorators, target, propertyKey, descriptor) {
        for (var i = decorators.length - 1; i >= 0; --i) {
            var decorator = decorators[i];
            var decorated = decorator(target, propertyKey, descriptor);
            if (!IsUndefined(decorated) && !IsNull(decorated)) {
                if (!IsObject(decorated))
                    throw new TypeError();
                descriptor = decorated;
            }
        }
        return descriptor;
    }
    function GetOrCreateMetadataMap(O, P, Create) {
        var targetMetadata = Metadata.get(O);
        if (IsUndefined(targetMetadata)) {
            if (!Create)
                return undefined;
            targetMetadata = new _Map();
            Metadata.set(O, targetMetadata);
        }
        var metadataMap = targetMetadata.get(P);
        if (IsUndefined(metadataMap)) {
            if (!Create)
                return undefined;
            metadataMap = new _Map();
            targetMetadata.set(P, metadataMap);
        }
        return metadataMap;
    }
    // 3.1.1.1 OrdinaryHasMetadata(MetadataKey, O, P)
    // https://rbuckton.github.io/reflect-metadata/#ordinaryhasmetadata
    function OrdinaryHasMetadata(MetadataKey, O, P) {
        var hasOwn = OrdinaryHasOwnMetadata(MetadataKey, O, P);
        if (hasOwn)
            return true;
        var parent = OrdinaryGetPrototypeOf(O);
        if (!IsNull(parent))
            return OrdinaryHasMetadata(MetadataKey, parent, P);
        return false;
    }
    // 3.1.2.1 OrdinaryHasOwnMetadata(MetadataKey, O, P)
    // https://rbuckton.github.io/reflect-metadata/#ordinaryhasownmetadata
    function OrdinaryHasOwnMetadata(MetadataKey, O, P) {
        var metadataMap = GetOrCreateMetadataMap(O, P, /*Create*/ false);
        if (IsUndefined(metadataMap))
            return false;
        return ToBoolean(metadataMap.has(MetadataKey));
    }
    // 3.1.3.1 OrdinaryGetMetadata(MetadataKey, O, P)
    // https://rbuckton.github.io/reflect-metadata/#ordinarygetmetadata
    function OrdinaryGetMetadata(MetadataKey, O, P) {
        var hasOwn = OrdinaryHasOwnMetadata(MetadataKey, O, P);
        if (hasOwn)
            return OrdinaryGetOwnMetadata(MetadataKey, O, P);
        var parent = OrdinaryGetPrototypeOf(O);
        if (!IsNull(parent))
            return OrdinaryGetMetadata(MetadataKey, parent, P);
        return undefined;
    }
    // 3.1.4.1 OrdinaryGetOwnMetadata(MetadataKey, O, P)
    // https://rbuckton.github.io/reflect-metadata/#ordinarygetownmetadata
    function OrdinaryGetOwnMetadata(MetadataKey, O, P) {
        var metadataMap = GetOrCreateMetadataMap(O, P, /*Create*/ false);
        if (IsUndefined(metadataMap))
            return undefined;
        return metadataMap.get(MetadataKey);
    }
    // 3.1.5.1 OrdinaryDefineOwnMetadata(MetadataKey, MetadataValue, O, P)
    // https://rbuckton.github.io/reflect-metadata/#ordinarydefineownmetadata
    function OrdinaryDefineOwnMetadata(MetadataKey, MetadataValue, O, P) {
        var metadataMap = GetOrCreateMetadataMap(O, P, /*Create*/ true);
        metadataMap.set(MetadataKey, MetadataValue);
    }
    // 3.1.6.1 OrdinaryMetadataKeys(O, P)
    // https://rbuckton.github.io/reflect-metadata/#ordinarymetadatakeys
    function OrdinaryMetadataKeys(O, P) {
        var ownKeys = OrdinaryOwnMetadataKeys(O, P);
        var parent = OrdinaryGetPrototypeOf(O);
        if (parent === null)
            return ownKeys;
        var parentKeys = OrdinaryMetadataKeys(parent, P);
        if (parentKeys.length <= 0)
            return ownKeys;
        if (ownKeys.length <= 0)
            return parentKeys;
        var set = new _Set();
        var keys = [];
        for (var _i = 0, ownKeys_1 = ownKeys; _i < ownKeys_1.length; _i++) {
            var key = ownKeys_1[_i];
            var hasKey = set.has(key);
            if (!hasKey) {
                set.add(key);
                keys.push(key);
            }
        }
        for (var _a = 0, parentKeys_1 = parentKeys; _a < parentKeys_1.length; _a++) {
            var key = parentKeys_1[_a];
            var hasKey = set.has(key);
            if (!hasKey) {
                set.add(key);
                keys.push(key);
            }
        }
        return keys;
    }
    // 3.1.7.1 OrdinaryOwnMetadataKeys(O, P)
    // https://rbuckton.github.io/reflect-metadata/#ordinaryownmetadatakeys
    function OrdinaryOwnMetadataKeys(O, P) {
        var keys = [];
        var metadataMap = GetOrCreateMetadataMap(O, P, /*Create*/ false);
        if (IsUndefined(metadataMap))
            return keys;
        var keysObj = metadataMap.keys();
        var iterator = GetIterator(keysObj);
        var k = 0;
        while (true) {
            var next = IteratorStep(iterator);
            if (!next) {
                keys.length = k;
                return keys;
            }
            var nextValue = IteratorValue(next);
            try {
                keys[k] = nextValue;
            }
            catch (e) {
                try {
                    IteratorClose(iterator);
                }
                finally {
                    throw e;
                }
            }
            k++;
        }
    }
    // 6 ECMAScript Data Typ0es and Values
    // https://tc39.github.io/ecma262/#sec-ecmascript-data-types-and-values
    function Type(x) {
        if (x === null)
            return 1 /* Null */;
        switch (typeof x) {
            case "undefined": return 0 /* Undefined */;
            case "boolean": return 2 /* Boolean */;
            case "string": return 3 /* String */;
            case "symbol": return 4 /* Symbol */;
            case "number": return 5 /* Number */;
            case "object": return x === null ? 1 /* Null */ : 6 /* Object */;
            default: return 6 /* Object */;
        }
    }
    // 6.1.1 The Undefined Type
    // https://tc39.github.io/ecma262/#sec-ecmascript-language-types-undefined-type
    function IsUndefined(x) {
        return x === undefined;
    }
    // 6.1.2 The Null Type
    // https://tc39.github.io/ecma262/#sec-ecmascript-language-types-null-type
    function IsNull(x) {
        return x === null;
    }
    // 6.1.5 The Symbol Type
    // https://tc39.github.io/ecma262/#sec-ecmascript-language-types-symbol-type
    function IsSymbol(x) {
        return typeof x === "symbol";
    }
    // 6.1.7 The Object Type
    // https://tc39.github.io/ecma262/#sec-object-type
    function IsObject(x) {
        return typeof x === "object" ? x !== null : typeof x === "function";
    }
    // 7.1 Type Conversion
    // https://tc39.github.io/ecma262/#sec-type-conversion
    // 7.1.1 ToPrimitive(input [, PreferredType])
    // https://tc39.github.io/ecma262/#sec-toprimitive
    function ToPrimitive(input, PreferredType) {
        switch (Type(input)) {
            case 0 /* Undefined */: return input;
            case 1 /* Null */: return input;
            case 2 /* Boolean */: return input;
            case 3 /* String */: return input;
            case 4 /* Symbol */: return input;
            case 5 /* Number */: return input;
        }
        var hint = PreferredType === 3 /* String */ ? "string" : PreferredType === 5 /* Number */ ? "number" : "default";
        var exoticToPrim = GetMethod(input, toPrimitiveSymbol);
        if (exoticToPrim !== undefined) {
            var result = exoticToPrim.call(input, hint);
            if (IsObject(result))
                throw new TypeError();
            return result;
        }
        return OrdinaryToPrimitive(input, hint === "default" ? "number" : hint);
    }
    // 7.1.1.1 OrdinaryToPrimitive(O, hint)
    // https://tc39.github.io/ecma262/#sec-ordinarytoprimitive
    function OrdinaryToPrimitive(O, hint) {
        if (hint === "string") {
            var toString_1 = O.toString;
            if (IsCallable(toString_1)) {
                var result = toString_1.call(O);
                if (!IsObject(result))
                    return result;
            }
            var valueOf = O.valueOf;
            if (IsCallable(valueOf)) {
                var result = valueOf.call(O);
                if (!IsObject(result))
                    return result;
            }
        }
        else {
            var valueOf = O.valueOf;
            if (IsCallable(valueOf)) {
                var result = valueOf.call(O);
                if (!IsObject(result))
                    return result;
            }
            var toString_2 = O.toString;
            if (IsCallable(toString_2)) {
                var result = toString_2.call(O);
                if (!IsObject(result))
                    return result;
            }
        }
        throw new TypeError();
    }
    // 7.1.2 ToBoolean(argument)
    // https://tc39.github.io/ecma262/2016/#sec-toboolean
    function ToBoolean(argument) {
        return !!argument;
    }
    // 7.1.12 ToString(argument)
    // https://tc39.github.io/ecma262/#sec-tostring
    function ToString(argument) {
        return "" + argument;
    }
    // 7.1.14 ToPropertyKey(argument)
    // https://tc39.github.io/ecma262/#sec-topropertykey
    function ToPropertyKey(argument) {
        var key = ToPrimitive(argument, 3 /* String */);
        if (IsSymbol(key))
            return key;
        return ToString(key);
    }
    // 7.2 Testing and Comparison Operations
    // https://tc39.github.io/ecma262/#sec-testing-and-comparison-operations
    // 7.2.2 IsArray(argument)
    // https://tc39.github.io/ecma262/#sec-isarray
    function IsArray(argument) {
        return Array.isArray
            ? Array.isArray(argument)
            : argument instanceof Object
                ? argument instanceof Array
                : Object.prototype.toString.call(argument) === "[object Array]";
    }
    // 7.2.3 IsCallable(argument)
    // https://tc39.github.io/ecma262/#sec-iscallable
    function IsCallable(argument) {
        // NOTE: This is an approximation as we cannot check for [[Call]] internal method.
        return typeof argument === "function";
    }
    // 7.2.4 IsConstructor(argument)
    // https://tc39.github.io/ecma262/#sec-isconstructor
    function IsConstructor(argument) {
        // NOTE: This is an approximation as we cannot check for [[Construct]] internal method.
        return typeof argument === "function";
    }
    // 7.2.7 IsPropertyKey(argument)
    // https://tc39.github.io/ecma262/#sec-ispropertykey
    function IsPropertyKey(argument) {
        switch (Type(argument)) {
            case 3 /* String */: return true;
            case 4 /* Symbol */: return true;
            default: return false;
        }
    }
    // 7.3 Operations on Objects
    // https://tc39.github.io/ecma262/#sec-operations-on-objects
    // 7.3.9 GetMethod(V, P)
    // https://tc39.github.io/ecma262/#sec-getmethod
    function GetMethod(V, P) {
        var func = V[P];
        if (func === undefined || func === null)
            return undefined;
        if (!IsCallable(func))
            throw new TypeError();
        return func;
    }
    // 7.4 Operations on Iterator Objects
    // https://tc39.github.io/ecma262/#sec-operations-on-iterator-objects
    function GetIterator(obj) {
        var method = GetMethod(obj, iteratorSymbol);
        if (!IsCallable(method))
            throw new TypeError(); // from Call
        var iterator = method.call(obj);
        if (!IsObject(iterator))
            throw new TypeError();
        return iterator;
    }
    // 7.4.4 IteratorValue(iterResult)
    // https://tc39.github.io/ecma262/2016/#sec-iteratorvalue
    function IteratorValue(iterResult) {
        return iterResult.value;
    }
    // 7.4.5 IteratorStep(iterator)
    // https://tc39.github.io/ecma262/#sec-iteratorstep
    function IteratorStep(iterator) {
        var result = iterator.next();
        return result.done ? false : result;
    }
    // 7.4.6 IteratorClose(iterator, completion)
    // https://tc39.github.io/ecma262/#sec-iteratorclose
    function IteratorClose(iterator) {
        var f = iterator["return"];
        if (f)
            f.call(iterator);
    }
    // 9.1 Ordinary Object Internal Methods and Internal Slots
    // https://tc39.github.io/ecma262/#sec-ordinary-object-internal-methods-and-internal-slots
    // 9.1.1.1 OrdinaryGetPrototypeOf(O)
    // https://tc39.github.io/ecma262/#sec-ordinarygetprototypeof
    function OrdinaryGetPrototypeOf(O) {
        var proto = Object.getPrototypeOf(O);
        if (typeof O !== "function" || O === functionPrototype)
            return proto;
        // TypeScript doesn't set __proto__ in ES5, as it's non-standard.
        // Try to determine the superclass constructor. Compatible implementations
        // must either set __proto__ on a subclass constructor to the superclass constructor,
        // or ensure each class has a valid `constructor` property on its prototype that
        // points back to the constructor.
        // If this is not the same as Function.[[Prototype]], then this is definately inherited.
        // This is the case when in ES6 or when using __proto__ in a compatible browser.
        if (proto !== functionPrototype)
            return proto;
        // If the super prototype is Object.prototype, null, or undefined, then we cannot determine the heritage.
        var prototype = O.prototype;
        var prototypeProto = prototype && Object.getPrototypeOf(prototype);
        if (prototypeProto == null || prototypeProto === Object.prototype)
            return proto;
        // If the constructor was not a function, then we cannot determine the heritage.
        var constructor = prototypeProto.constructor;
        if (typeof constructor !== "function")
            return proto;
        // If we have some kind of self-reference, then we cannot determine the heritage.
        if (constructor === O)
            return proto;
        // we have a pretty good guess at the heritage.
        return constructor;
    }
    // naive Map shim
    function CreateMapPolyfill() {
        var cacheSentinel = {};
        var arraySentinel = [];
        var MapIterator = (function () {
            function MapIterator(keys, values, selector) {
                this._index = 0;
                this._keys = keys;
                this._values = values;
                this._selector = selector;
            }
            MapIterator.prototype["@@iterator"] = function () { return this; };
            MapIterator.prototype[iteratorSymbol] = function () { return this; };
            MapIterator.prototype.next = function () {
                var index = this._index;
                if (index >= 0 && index < this._keys.length) {
                    var result = this._selector(this._keys[index], this._values[index]);
                    if (index + 1 >= this._keys.length) {
                        this._index = -1;
                        this._keys = arraySentinel;
                        this._values = arraySentinel;
                    }
                    else {
                        this._index++;
                    }
                    return { value: result, done: false };
                }
                return { value: undefined, done: true };
            };
            MapIterator.prototype.throw = function (error) {
                if (this._index >= 0) {
                    this._index = -1;
                    this._keys = arraySentinel;
                    this._values = arraySentinel;
                }
                throw error;
            };
            MapIterator.prototype.return = function (value) {
                if (this._index >= 0) {
                    this._index = -1;
                    this._keys = arraySentinel;
                    this._values = arraySentinel;
                }
                return { value: value, done: true };
            };
            return MapIterator;
        }());
        return (function () {
            function Map() {
                this._keys = [];
                this._values = [];
                this._cacheKey = cacheSentinel;
                this._cacheIndex = -2;
            }
            Object.defineProperty(Map.prototype, "size", {
                get: function () { return this._keys.length; },
                enumerable: true,
                configurable: true
            });
            Map.prototype.has = function (key) { return this._find(key, /*insert*/ false) >= 0; };
            Map.prototype.get = function (key) {
                var index = this._find(key, /*insert*/ false);
                return index >= 0 ? this._values[index] : undefined;
            };
            Map.prototype.set = function (key, value) {
                var index = this._find(key, /*insert*/ true);
                this._values[index] = value;
                return this;
            };
            Map.prototype.delete = function (key) {
                var index = this._find(key, /*insert*/ false);
                if (index >= 0) {
                    var size = this._keys.length;
                    for (var i = index + 1; i < size; i++) {
                        this._keys[i - 1] = this._keys[i];
                        this._values[i - 1] = this._values[i];
                    }
                    this._keys.length--;
                    this._values.length--;
                    if (key === this._cacheKey) {
                        this._cacheKey = cacheSentinel;
                        this._cacheIndex = -2;
                    }
                    return true;
                }
                return false;
            };
            Map.prototype.clear = function () {
                this._keys.length = 0;
                this._values.length = 0;
                this._cacheKey = cacheSentinel;
                this._cacheIndex = -2;
            };
            Map.prototype.keys = function () { return new MapIterator(this._keys, this._values, getKey); };
            Map.prototype.values = function () { return new MapIterator(this._keys, this._values, getValue); };
            Map.prototype.entries = function () { return new MapIterator(this._keys, this._values, getEntry); };
            Map.prototype["@@iterator"] = function () { return this.entries(); };
            Map.prototype[iteratorSymbol] = function () { return this.entries(); };
            Map.prototype._find = function (key, insert) {
                if (this._cacheKey !== key) {
                    this._cacheIndex = this._keys.indexOf(this._cacheKey = key);
                }
                if (this._cacheIndex < 0 && insert) {
                    this._cacheIndex = this._keys.length;
                    this._keys.push(key);
                    this._values.push(undefined);
                }
                return this._cacheIndex;
            };
            return Map;
        }());
        function getKey(key, _) {
            return key;
        }
        function getValue(_, value) {
            return value;
        }
        function getEntry(key, value) {
            return [key, value];
        }
    }
    // naive Set shim
    function CreateSetPolyfill() {
        return (function () {
            function Set() {
                this._map = new _Map();
            }
            Object.defineProperty(Set.prototype, "size", {
                get: function () { return this._map.size; },
                enumerable: true,
                configurable: true
            });
            Set.prototype.has = function (value) { return this._map.has(value); };
            Set.prototype.add = function (value) { return this._map.set(value, value), this; };
            Set.prototype.delete = function (value) { return this._map.delete(value); };
            Set.prototype.clear = function () { this._map.clear(); };
            Set.prototype.keys = function () { return this._map.keys(); };
            Set.prototype.values = function () { return this._map.values(); };
            Set.prototype.entries = function () { return this._map.entries(); };
            Set.prototype["@@iterator"] = function () { return this.keys(); };
            Set.prototype[iteratorSymbol] = function () { return this.keys(); };
            return Set;
        }());
    }
    // naive WeakMap shim
    function CreateWeakMapPolyfill() {
        var UUID_SIZE = 16;
        var keys = HashMap.create();
        var rootKey = CreateUniqueKey();
        return (function () {
            function WeakMap() {
                this._key = CreateUniqueKey();
            }
            WeakMap.prototype.has = function (target) {
                var table = GetOrCreateWeakMapTable(target, /*create*/ false);
                return table !== undefined ? HashMap.has(table, this._key) : false;
            };
            WeakMap.prototype.get = function (target) {
                var table = GetOrCreateWeakMapTable(target, /*create*/ false);
                return table !== undefined ? HashMap.get(table, this._key) : undefined;
            };
            WeakMap.prototype.set = function (target, value) {
                var table = GetOrCreateWeakMapTable(target, /*create*/ true);
                table[this._key] = value;
                return this;
            };
            WeakMap.prototype.delete = function (target) {
                var table = GetOrCreateWeakMapTable(target, /*create*/ false);
                return table !== undefined ? delete table[this._key] : false;
            };
            WeakMap.prototype.clear = function () {
                // NOTE: not a real clear, just makes the previous data unreachable
                this._key = CreateUniqueKey();
            };
            return WeakMap;
        }());
        function CreateUniqueKey() {
            var key;
            do
                key = "@@WeakMap@@" + CreateUUID();
            while (HashMap.has(keys, key));
            keys[key] = true;
            return key;
        }
        function GetOrCreateWeakMapTable(target, create) {
            if (!hasOwn.call(target, rootKey)) {
                if (!create)
                    return undefined;
                Object.defineProperty(target, rootKey, { value: HashMap.create() });
            }
            return target[rootKey];
        }
        function FillRandomBytes(buffer, size) {
            for (var i = 0; i < size; ++i)
                buffer[i] = Math.random() * 0xff | 0;
            return buffer;
        }
        function GenRandomBytes(size) {
            if (typeof Uint8Array === "function") {
                if (typeof crypto !== "undefined")
                    return crypto.getRandomValues(new Uint8Array(size));
                if (typeof msCrypto !== "undefined")
                    return msCrypto.getRandomValues(new Uint8Array(size));
                return FillRandomBytes(new Uint8Array(size), size);
            }
            return FillRandomBytes(new Array(size), size);
        }
        function CreateUUID() {
            var data = GenRandomBytes(UUID_SIZE);
            // mark as random - RFC 4122 § 4.4
            data[6] = data[6] & 0x4f | 0x40;
            data[8] = data[8] & 0xbf | 0x80;
            var result = "";
            for (var offset = 0; offset < UUID_SIZE; ++offset) {
                var byte = data[offset];
                if (offset === 4 || offset === 6 || offset === 8)
                    result += "-";
                if (byte < 16)
                    result += "0";
                result += byte.toString(16).toLowerCase();
            }
            return result;
        }
    }
    // uses a heuristic used by v8 and chakra to force an object into dictionary mode.
    function MakeDictionary(obj) {
        obj.__ = undefined;
        delete obj.__;
        return obj;
    }
    // patch global Reflect
    (function (__global) {
        if (typeof __global.Reflect !== "undefined") {
            if (__global.Reflect !== Reflect) {
                for (var p in Reflect) {
                    if (hasOwn.call(Reflect, p)) {
                        __global.Reflect[p] = Reflect[p];
                    }
                }
            }
        }
        else {
            __global.Reflect = Reflect;
        }
    })(typeof global !== "undefined" ? global :
        typeof self !== "undefined" ? self :
            Function("return this;")());
})(Reflect || (Reflect = {}));
//# sourceMappingURL=Reflect.js.map
/* WEBPACK VAR INJECTION */}.call(exports, __webpack_require__(53), __webpack_require__(61)))

/***/ }),
/* 44 */
/***/ (function(module, exports, __webpack_require__) {

"use strict";

var ansiRegex = __webpack_require__(12)();

module.exports = function (str) {
	return typeof str === 'string' ? str.replace(ansiRegex, '') : str;
};


/***/ }),
/* 45 */
/***/ (function(module, exports, __webpack_require__) {


        var result = __webpack_require__(26);

        if (typeof result === "string") {
            module.exports = result;
        } else {
            module.exports = result.toString();
        }
    

/***/ }),
/* 46 */
/***/ (function(module, exports, __webpack_require__) {


        var result = __webpack_require__(27);

        if (typeof result === "string") {
            module.exports = result;
        } else {
            module.exports = result.toString();
        }
    

/***/ }),
/* 47 */
/***/ (function(module, exports, __webpack_require__) {


        var result = __webpack_require__(28);

        if (typeof result === "string") {
            module.exports = result;
        } else {
            module.exports = result.toString();
        }
    

/***/ }),
/* 48 */
/***/ (function(module, exports, __webpack_require__) {


        var result = __webpack_require__(29);

        if (typeof result === "string") {
            module.exports = result;
        } else {
            module.exports = result.toString();
        }
    

/***/ }),
/* 49 */
/***/ (function(module, exports, __webpack_require__) {

/*eslint-env browser*/

var clientOverlay = document.createElement('div');
clientOverlay.id = 'webpack-hot-middleware-clientOverlay';
var styles = {
  background: 'rgba(0,0,0,0.85)',
  color: '#E8E8E8',
  lineHeight: '1.2',
  whiteSpace: 'pre',
  fontFamily: 'Menlo, Consolas, monospace',
  fontSize: '13px',
  position: 'fixed',
  zIndex: 9999,
  padding: '10px',
  left: 0,
  right: 0,
  top: 0,
  bottom: 0,
  overflow: 'auto',
  dir: 'ltr',
  textAlign: 'left'
};
for (var key in styles) {
  clientOverlay.style[key] = styles[key];
}

var ansiHTML = __webpack_require__(11);
var colors = {
  reset: ['transparent', 'transparent'],
  black: '181818',
  red: 'E36049',
  green: 'B3CB74',
  yellow: 'FFD080',
  blue: '7CAFC2',
  magenta: '7FACCA',
  cyan: 'C3C2EF',
  lightgrey: 'EBE7E3',
  darkgrey: '6D7891'
};
ansiHTML.setColors(colors);

var Entities = __webpack_require__(30).AllHtmlEntities;
var entities = new Entities();

exports.showProblems =
function showProblems(type, lines) {
  clientOverlay.innerHTML = '';
  lines.forEach(function(msg) {
    msg = ansiHTML(entities.encode(msg));
    var div = document.createElement('div');
    div.style.marginBottom = '26px';
    div.innerHTML = problemType(type) + ' in ' + msg;
    clientOverlay.appendChild(div);
  });
  if (document.body) {
    document.body.appendChild(clientOverlay);
  }
};

exports.clear =
function clear() {
  if (document.body && clientOverlay.parentNode) {
    document.body.removeChild(clientOverlay);
  }
};

var problemColors = {
  errors: colors.red,
  warnings: colors.yellow
};

function problemType (type) {
  var color = problemColors[type] || colors.red;
  return (
    '<span style="background-color:#' + color + '; color:#fff; padding:2px 4px; border-radius: 2px">' +
      type.slice(0, -1).toUpperCase() +
    '</span>'
  );
}


/***/ }),
/* 50 */
/***/ (function(module, exports, __webpack_require__) {

/**
 * Based heavily on https://github.com/webpack/webpack/blob/
 *  c0afdf9c6abc1dd70707c594e473802a566f7b6e/hot/only-dev-server.js
 * Original copyright Tobias Koppers @sokra (MIT license)
 */

/* global window __webpack_hash__ */

if (false) {
  throw new Error("[HMR] Hot Module Replacement is disabled.");
}

var hmrDocsUrl = "http://webpack.github.io/docs/hot-module-replacement-with-webpack.html"; // eslint-disable-line max-len

var lastHash;
var failureStatuses = { abort: 1, fail: 1 };
var applyOptions = { ignoreUnaccepted: true };

function upToDate(hash) {
  if (hash) lastHash = hash;
  return lastHash == __webpack_require__.h();
}

module.exports = function(hash, moduleMap, options) {
  var reload = options.reload;
  if (!upToDate(hash) && module.hot.status() == "idle") {
    if (options.log) console.log("[HMR] Checking for updates on the server...");
    check();
  }

  function check() {
    var cb = function(err, updatedModules) {
      if (err) return handleError(err);

      if(!updatedModules) {
        if (options.warn) {
          console.warn("[HMR] Cannot find update (Full reload needed)");
          console.warn("[HMR] (Probably because of restarting the server)");
        }
        performReload();
        return null;
      }

      var applyCallback = function(applyErr, renewedModules) {
        if (applyErr) return handleError(applyErr);

        if (!upToDate()) check();

        logUpdates(updatedModules, renewedModules);
      };

      var applyResult = module.hot.apply(applyOptions, applyCallback);
      // webpack 2 promise
      if (applyResult && applyResult.then) {
        // HotModuleReplacement.runtime.js refers to the result as `outdatedModules`
        applyResult.then(function(outdatedModules) {
          applyCallback(null, outdatedModules);
        });
        applyResult.catch(applyCallback);
      }

    };

    var result = module.hot.check(false, cb);
    // webpack 2 promise
    if (result && result.then) {
        result.then(function(updatedModules) {
            cb(null, updatedModules);
        });
        result.catch(cb);
    }
  }

  function logUpdates(updatedModules, renewedModules) {
    var unacceptedModules = updatedModules.filter(function(moduleId) {
      return renewedModules && renewedModules.indexOf(moduleId) < 0;
    });

    if(unacceptedModules.length > 0) {
      if (options.warn) {
        console.warn(
          "[HMR] The following modules couldn't be hot updated: " +
          "(Full reload needed)\n" +
          "This is usually because the modules which have changed " +
          "(and their parents) do not know how to hot reload themselves. " +
          "See " + hmrDocsUrl + " for more details."
        );
        unacceptedModules.forEach(function(moduleId) {
          console.warn("[HMR]  - " + moduleMap[moduleId]);
        });
      }
      performReload();
      return;
    }

    if (options.log) {
      if(!renewedModules || renewedModules.length === 0) {
        console.log("[HMR] Nothing hot updated.");
      } else {
        console.log("[HMR] Updated modules:");
        renewedModules.forEach(function(moduleId) {
          console.log("[HMR]  - " + moduleMap[moduleId]);
        });
      }

      if (upToDate()) {
        console.log("[HMR] App is up to date.");
      }
    }
  }

  function handleError(err) {
    if (module.hot.status() in failureStatuses) {
      if (options.warn) {
        console.warn("[HMR] Cannot check for update (Full reload needed)");
        console.warn("[HMR] " + err.stack || err.message);
      }
      performReload();
      return;
    }
    if (options.warn) {
      console.warn("[HMR] Update check failed: " + err.stack || err.message);
    }
  }

  function performReload() {
    if (reload) {
      if (options.warn) console.warn("[HMR] Reloading page");
      window.location.reload();
    }
  }
};


/***/ }),
/* 51 */
/***/ (function(module, exports) {

module.exports = function(module) {
	if(!module.webpackPolyfill) {
		module.deprecate = function() {};
		module.paths = [];
		// module.parent = undefined by default
		if(!module.children) module.children = [];
		Object.defineProperty(module, "loaded", {
			enumerable: true,
			get: function() {
				return module.l;
			}
		});
		Object.defineProperty(module, "id", {
			enumerable: true,
			get: function() {
				return module.i;
			}
		});
		module.webpackPolyfill = 1;
	}
	return module;
};


/***/ }),
/* 52 */
/***/ (function(module, exports, __webpack_require__) {

module.exports = (__webpack_require__(0))(0);

/***/ }),
/* 53 */
/***/ (function(module, exports, __webpack_require__) {

module.exports = (__webpack_require__(0))(23);

/***/ }),
/* 54 */
/***/ (function(module, exports, __webpack_require__) {

module.exports = (__webpack_require__(0))(28);

/***/ }),
/* 55 */
/***/ (function(module, exports, __webpack_require__) {

module.exports = (__webpack_require__(0))(38);

/***/ }),
/* 56 */
/***/ (function(module, exports, __webpack_require__) {

module.exports = (__webpack_require__(0))(40);

/***/ }),
/* 57 */
/***/ (function(module, exports, __webpack_require__) {

module.exports = (__webpack_require__(0))(41);

/***/ }),
/* 58 */
/***/ (function(module, exports, __webpack_require__) {

module.exports = (__webpack_require__(0))(42);

/***/ }),
/* 59 */
/***/ (function(module, exports, __webpack_require__) {

module.exports = (__webpack_require__(0))(47);

/***/ }),
/* 60 */
/***/ (function(module, exports, __webpack_require__) {

module.exports = (__webpack_require__(0))(6);

/***/ }),
/* 61 */
/***/ (function(module, exports, __webpack_require__) {

module.exports = (__webpack_require__(0))(8);

/***/ }),
/* 62 */
/***/ (function(module, exports, __webpack_require__) {

module.exports = (__webpack_require__(0))(9);

/***/ }),
/* 63 */
/***/ (function(module, exports, __webpack_require__) {

__webpack_require__(10);
__webpack_require__(9);
module.exports = __webpack_require__(8);


/***/ })
/******/ ]);
//# sourceMappingURL=main-client.js.map