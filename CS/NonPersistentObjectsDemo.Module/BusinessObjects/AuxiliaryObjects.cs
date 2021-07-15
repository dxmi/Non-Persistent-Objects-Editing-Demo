﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DevExpress.Data.Filtering;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.DC;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.Validation;

namespace NonPersistentObjectsDemo.Module.BusinessObjects {

    [DefaultClassOptions]
    [DefaultListViewOptions(true, NewItemRowPosition.Top)]
    [DefaultProperty("Subject")]
    [DevExpress.ExpressApp.DC.DomainComponent]
    public class Message : NonPersistentObjectBase {
        private int id;
        [Browsable(false)]
        [DevExpress.ExpressApp.Data.Key]
        public int ID {
            get { return id; }
        }
        public void SetKey(int id) {
            this.id = id;
        }
        private Account _Sender;
        public Account Sender {
            get { return _Sender; }
            set { SetPropertyValue(nameof(Sender), ref _Sender, value); }
        }
        private Account _Recepient;
        public Account Recepient {
            get { return _Recepient; }
            set { SetPropertyValue(nameof(Recepient), ref _Recepient, value); }
        }
        private string _Subject;
        public string Subject {
            get { return _Subject; }
            set { SetPropertyValue<string>(nameof(Subject), ref _Subject, value); }
        }
        private string _Body;
        [FieldSize(-1)]
        public string Body {
            get { return _Body; }
            set { SetPropertyValue<string>(nameof(Body), ref _Body, value); }
        }
    }

    [DefaultClassOptions]
    [DefaultListViewOptions(true, NewItemRowPosition.Top)]
    [DefaultProperty("PublicName")]
    [DevExpress.ExpressApp.DC.DomainComponent]
    public class Account : NonPersistentObjectBase {
        private string userName;
        //[Browsable(false)]
        [DevExpress.ExpressApp.ConditionalAppearance.Appearance("", Enabled = false, Criteria = "Not IsNewObject(This)")]
        [RuleRequiredField]
        [DevExpress.ExpressApp.Data.Key]
        public string UserName {
            get { return userName; }
            set { userName = value; }
        }
        public void SetKey(string userName) {
            this.userName = userName;
        }
        private string publicName;
        public string PublicName {
            get { return publicName; }
            set { SetPropertyValue(nameof(PublicName), ref publicName, value); }
        }

        private BindingList<Message> _messagesSent;
        public BindingList<Message> MessagesSent {
            get {
                if(_messagesSent == null) {
                    _messagesSent = new BindingList<Message>();
                    CriteriaOperator criteria = new BinaryOperator(
                        new OperandProperty("Sender.UserName"), new OperandValue(UserName),
                        BinaryOperatorType.Equal
                    );
                    var mgs = ObjectSpace.GetObjects<Message>(criteria);
                    foreach(var m in mgs) {
                        _messagesSent.Add(m);
                    }
                }
                return _messagesSent;
            }
        }
    }


    public class PostOfficeFactory : NonPersistentObjectFactoryBase {
        private PostOfficeClient Storage => GlobalServiceProvider<PostOfficeClient>.GetService();
        private bool isLoading = false;
        private ObjectMap objectMap;
        public PostOfficeFactory(ObjectMap objectMap) {
            this.objectMap = objectMap;
        }
        public override object GetObjectByKey(Type objectType, object key) {
            if(key == null) {
                throw new ArgumentNullException(nameof(key));
            }
            if(Storage.Mappings.TryGetValue(objectType, out var mapping)) {
                return WrapLoading(() => {
                    var loader = new DataStoreObjectLoader(Storage.Mappings, Storage.DataStore, objectMap);
                    return loader.LoadObjectByKey(objectType, key);
                });
            }
            throw new NotImplementedException();
        }
        public override IEnumerable GetObjects(Type objectType, CriteriaOperator criteria, IList<DevExpress.Xpo.SortProperty> sorting) {
            if(Storage.Mappings.TryGetValue(objectType, out var mapping)) {
                return WrapLoading(() => {
                    var loader = new DataStoreObjectLoader(Storage.Mappings, Storage.DataStore, objectMap);
                    return loader.LoadObjects(objectType, criteria);
                });
            }
            throw new NotImplementedException();
        }
        private T WrapLoading<T>(Func<T> doer) {
            if(isLoading) {
                throw new InvalidOperationException();
            }
            isLoading = true;
            try {
                return doer.Invoke();
            }
            finally {
                isLoading = false;
            }
        }
        public override void SaveObjects(ICollection toInsert, ICollection toUpdate, ICollection toDelete) {
            var saver = new DataStoreObjectSaver(Storage.Mappings, Storage.DataStore);
            saver.SaveObjects(toInsert, toUpdate, toDelete);
        }
    }
}
