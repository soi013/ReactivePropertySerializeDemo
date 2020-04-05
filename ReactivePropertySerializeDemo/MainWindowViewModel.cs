using MessagePack;
using MessagePack.ReactivePropertyExtension;
using Reactive.Bindings;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace ReactivePropertySerializeDemo
{
    public class MainWindowViewModel : INotifyPropertyChanged
    {
        //メモリリークを防ぐためのダミー実装
        public event PropertyChangedEventHandler PropertyChanged;

        public RpNames Names { get; } = new RpNames();

        public ReactiveProperty<string> MessagePackSerializedNames { get; } = new ReactiveProperty<string>();
        public ReactiveProperty<string> JsonSerializedNames { get; } = new ReactiveProperty<string>();

        public ReactiveCommand SerializeCommand { get; }
        public ReactiveCommand DesirializeCommand { get; }

        public MainWindowViewModel()
        {
            SerializeCommand = Names.NameRorps
                .Select(x => x?.Length >= 3)
                .ToReactiveCommand()
                .WithSubscribe(() => Serialize());

            DesirializeCommand = JsonSerializedNames
                .Select(x => x?.Length > 5)
                .ToReactiveCommand()
                .WithSubscribe(() => Desirialize());

            //ReactiveProperty用を含んだResolverのセットをデフォルトに設定しておく
            var resolver = MessagePack.Resolvers.CompositeResolver.Create(
                ReactivePropertyResolver.Instance,
                MessagePack.Resolvers.ContractlessStandardResolver.Instance,
                MessagePack.Resolvers.StandardResolver.Instance
            );
            MessagePackSerializer.DefaultOptions = MessagePack.MessagePackSerializerOptions.Standard.WithResolver(resolver);
        }

        private void Serialize()
        {
            var messagePackNames = MessagePackSerializer.Serialize(Names);
            this.MessagePackSerializedNames.Value = String.Join(" ",
                messagePackNames.Select(x => $"{x:X2}"));

            this.JsonSerializedNames.Value = MessagePackSerializer.SerializeToJson(Names);
        }

        private void Desirialize()
        {
            //JSON側からデシリアライズ
            var mPack = MessagePack.MessagePackSerializer.ConvertFromJson(JsonSerializedNames.Value);
            var deserializedRPNames = MessagePackSerializer.Deserialize<RpNames>(mPack);

            this.Names.NameRp.Value = deserializedRPNames.NameRp.Value;
            this.Names.NameRps.Value = deserializedRPNames.NameRps.Value;
        }
    }

    public class RpNames
    {
        //{get;}だけだとSerializeされない
        public ReactiveProperty<string> NameRp { get; set; } = new ReactiveProperty<string>("Anakin");

        //ReactivePropertyでもReactivePropertySlimでもできる。
        public ReactivePropertySlim<string> NameRps { get; set; } = new ReactivePropertySlim<string>("Skywalker");

        //ReadOnlyはSerializeできない。
        [IgnoreMember]
        public ReadOnlyReactivePropertySlim<string> NameRorps { get; set; }

        public RpNames()
        {
            //姓と名の変更を購読して、フルネームにする
            NameRorps = Observable
                .CombineLatest(NameRp, NameRps, (x, y) => $"{x}={y}")
                .ToReadOnlyReactivePropertySlim();
        }
    }
}
